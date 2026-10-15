using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.CloseAuction;

public class CloseAuctionCommandHandler : IRequestHandler<CloseAuctionCommand>
{
    public const decimal CommissionRate = 0.05m;
    public const int PaymentDueDays = 3;

    private readonly IAuctionRepository _auctionRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseAuctionCommandHandler(
        IAuctionRepository auctionRepository,
        IPaymentRepository paymentRepository,
        INotificationRepository notificationRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _paymentRepository = paymentRepository;
        _notificationRepository = notificationRepository;
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CloseAuctionCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken);
            var now = DateTime.UtcNow;

            // Ponovna provera pod lock-om: aukcija je možda već zatvorena,
            // ili je anti-snipe ponuda u međuvremenu produžila kraj.
            if (auction is null || auction.Status != AuctionStatus.Active || auction.EndTime > now)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            var hasWinner = auction.CurrentWinnerId.HasValue;
            var reserveMet = auction.ReservePrice is null || auction.CurrentPrice >= auction.ReservePrice;
            var sold = hasWinner && reserveMet;

            auction.Status = AuctionStatus.Ended;
            if (!sold)
                auction.CurrentWinnerId = null; // nema pobednika

            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            string? wonMessage = null;
            if (sold)
            {
                var winnerId = auction.CurrentWinnerId!.Value;

                await _paymentRepository.AddAsync(new Payment
                {
                    Id = Guid.NewGuid(),
                    AuctionId = auction.Id,
                    WinnerId = winnerId,
                    Amount = auction.CurrentPrice,
                    Commission = Math.Round(auction.CurrentPrice * CommissionRate, 2),
                    Status = PaymentStatus.Pending,
                    DueDate = now.AddDays(PaymentDueDays)
                }, cancellationToken);

                var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken);
                var title = item?.Title ?? "predmet";

                wonMessage = $"Čestitamo! Pobedili ste aukciju za „{title}“ sa cenom {auction.CurrentPrice:N2}. " +
                             $"Plaćanje je potrebno izvršiti u roku od {PaymentDueDays} dana.";

                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = winnerId,
                    Type = NotificationType.AuctionWon,
                    Message = wonMessage
                }, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}