using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Bids.Commands.PlaceBid;

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, Guid>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRepository _notificationRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUserRepository _userRepository;

    public PlaceBidCommandHandler(
        IAuctionRepository auctionRepository,
        IBidRepository bidRepository,
        IUnitOfWork unitOfWork,
        INotificationRepository notificationRepository,
        IItemRepository itemRepository,
        IUserRepository userRepository)
    {
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
        _unitOfWork = unitOfWork;
        _notificationRepository = notificationRepository;
        _itemRepository = itemRepository;
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        if (request.ProxyMaxAmount.HasValue && request.ProxyMaxAmount.Value < request.Amount)
            throw new ArgumentException("Maksimalna proxy ponuda ne moze biti manja od iznosa ponude");

        // Samo verifikovani korisnici sa registrovanim načinom plaćanja mogu da licitiraju.
        var bidder = await _userRepository.GetByIdAsync(request.BidderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Korisnik {request.BidderId} ne postoji");

        if (!bidder.IsVerified)
            throw new ForbiddenAccessException("Samo verifikovani korisnici mogu da se nadmeću");

        if (!bidder.HasPaymentMethod)
            throw new ForbiddenAccessException("Pre nadmetanja je potrebno dodati način plaćanja");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // BEGIN -> SELECT FOR UPDATE -> validate -> INSERT bids -> UPDATE auction -> COMMIT
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

            if (auction.Status != AuctionStatus.Active)
                throw new InvalidOperationException(
                    $"Nadmetanje nije moguce, aukcija je u statusu {auction.Status}");

            var now = DateTime.UtcNow;
            if (now < auction.StartTime || now >= auction.EndTime)
                throw new InvalidOperationException("Aukcija trenutno nije otvorena za nadmetanje");

            if (auction.CurrentWinnerId == request.BidderId)
                throw new InvalidOperationException("Vec ste trenutni vodeci ponudjac");

            // Prodavac ne sme da diže cenu sopstvenoj aukciji (korisnik može biti i Seller i Bidder).
            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken);
            if (item is not null && item.SellerId == request.BidderId)
                throw new InvalidOperationException("Ne možete se nadmetati na sopstvenoj aukciji");

            // ---- Holandska aukcija: cena pada tokom vremena, prvi koji prihvati cenu pobeđuje ----
            if (auction.Type == AuctionType.Dutch)
            {
                var dutchPrice = DutchPricing.CurrentPrice(auction, now);

                if (request.Amount < dutchPrice)
                    throw new ArgumentException($"Trenutna cena je {dutchPrice}, ponuda mora biti najmanje toliko");

                var dutchBid = new Bid
                {
                    Id = Guid.NewGuid(),
                    AuctionId = auction.Id,
                    BidderId = request.BidderId,
                    Amount = dutchPrice,
                    IsProxy = false,
                    CreatedAt = now
                };

                await _bidRepository.AddRangeAsync(new[] { dutchBid }, cancellationToken);

                auction.CurrentPrice = dutchPrice;
                auction.CurrentWinnerId = request.BidderId;
                auction.EndTime = now; // AuctionClosingWorker zatvara aukciju i pravi Payment

                await _auctionRepository.UpdateAsync(auction, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return dutchBid.Id;
            }

            var previousWinnerId = auction.CurrentWinnerId;

            var minimumBid = auction.CurrentWinnerId is null
                ? auction.StartingPrice
                : auction.CurrentPrice + auction.BidIncrement;

            if (request.Amount < minimumBid)
                throw new ArgumentException($"Ponuda mora biti najmanje {minimumBid}");

            // ---- Proxy bid logika ----
            var amount = request.Amount;
            var maxAmount = request.ProxyMaxAmount ?? request.Amount; // tajni maksimum novog biddera
            var increment = auction.BidIncrement;

            var bids = new List<Bid>();
            var tick = 0;

            // Svaki red dobija malo veci timestamp da redosled u istoriji ostane jasan.
            Bid NewBid(Guid bidderId, decimal bidAmount, decimal? proxyMax, bool isProxy) => new()
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                BidderId = bidderId,
                Amount = bidAmount,
                IsProxy = isProxy,
                ProxyMaxAmount = proxyMax,
                CreatedAt = now.AddTicks(tick++)
            };

            var firstBid = NewBid(request.BidderId, amount, request.ProxyMaxAmount, false);
            bids.Add(firstBid);

            decimal newPrice;
            Guid newWinnerId;

            if (auction.CurrentWinnerId is null)
            {
                // Prva ponuda na aukciji.
                newPrice = amount;
                newWinnerId = request.BidderId;
            }
            else
            {
                var leaderId = auction.CurrentWinnerId.Value;
                var leaderMax = await _bidRepository.GetMaxAmountForBidderAsync(auction.Id, leaderId, cancellationToken)
                    ?? auction.CurrentPrice;

                if (maxAmount > leaderMax)
                {
                    // Novi bidder pobedjuje: placa minimum potreban da nadmasi tajni maksimum vodeceg.
                    newPrice = Math.Max(amount, Math.Min(maxAmount, leaderMax + increment));
                    newWinnerId = request.BidderId;

                    // Ako je vodeci imao maksimum >= ponude, njegov proxy se automatski oglasio do maksimuma.
                    var lastAmount = amount;
                    if (leaderMax >= amount)
                    {
                        bids.Add(NewBid(leaderId, leaderMax, leaderMax, true));
                        lastAmount = leaderMax;
                    }

                    // Novi bidder (proxy) se podize na konacnu cenu.
                    if (newPrice > lastAmount)
                        bids.Add(NewBid(request.BidderId, newPrice, maxAmount, true));
                }
                else
                {
                    // Vodeci zadrzava vodjstvo (kod izjednacenja pobedjuje raniji): njegov proxy nadmasuje novu ponudu.
                    newPrice = Math.Min(leaderMax, maxAmount + increment);
                    newWinnerId = leaderId;
                    bids.Add(NewBid(leaderId, newPrice, leaderMax, true));
                }
            }

            await _bidRepository.AddRangeAsync(bids, cancellationToken);

            auction.CurrentPrice = newPrice;
            auction.CurrentWinnerId = newWinnerId;

            // ---- Anti-snipe ----
            if (auction.EndTime - now <= TimeSpan.FromMinutes(auction.AntiSnipeWindowMinutes))
            {
                auction.EndTime = auction.EndTime.AddMinutes(auction.AntiSnipeExtensionMinutes);
            }

            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            // Obaveštenje bivšem lideru da je nadmašen (isti commit kao i ponuda).
            string? outbidMessage = null;
            if (previousWinnerId.HasValue && previousWinnerId.Value != newWinnerId)
            {
                outbidMessage = $"Neko vas je nadmasio na aukciji „{item?.Title ?? "predmet"}“. Trenutna cena: {newPrice:N2}.";

                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = previousWinnerId.Value,
                    Type = NotificationType.Outbid,
                    Message = outbidMessage
                }, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            return firstBid.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}