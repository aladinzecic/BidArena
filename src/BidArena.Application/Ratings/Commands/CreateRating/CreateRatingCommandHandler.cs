using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Ratings.Commands.CreateRating;

public class CreateRatingCommandHandler : IRequestHandler<CreateRatingCommand, Guid>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IRatingRepository _ratingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRatingCommandHandler(
        IAuctionRepository auctionRepository,
        IRatingRepository ratingRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _ratingRepository = ratingRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateRatingCommand request, CancellationToken cancellationToken)
    {
        var data = await _auctionRepository.GetWithItemAsync(request.AuctionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        if (data.Auction.Status != AuctionStatus.Settled)
            throw new InvalidOperationException(
                $"Ocenjivanje je moguće tek nakon završetka aukcije (Settled), trenutni status je {data.Auction.Status}");

        var sellerId = data.Item.SellerId;
        var buyerId = data.Auction.CurrentWinnerId
            ?? throw new InvalidOperationException("Aukcija nema pobednika");

        Guid ratedUserId;
        if (request.RaterId == buyerId)
            ratedUserId = sellerId;
        else if (request.RaterId == sellerId)
            ratedUserId = buyerId;
        else
            throw new ForbiddenAccessException("Aukciju mogu da ocene samo prodavac i pobednik");

        if (await _ratingRepository.ExistsAsync(request.AuctionId, request.RaterId, cancellationToken))
            throw new InvalidOperationException("Već ste ocenili ovu aukciju");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var rated = await _userRepository.GetByIdAsync(ratedUserId, cancellationToken)
                ?? throw new KeyNotFoundException($"Korisnik {ratedUserId} ne postoji");

            var rating = new Rating
            {
                Id = Guid.NewGuid(),
                AuctionId = request.AuctionId,
                RaterId = request.RaterId,
                RatedUserId = ratedUserId,
                Score = request.Score,
                Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
            };

            await _ratingRepository.AddAsync(rating, cancellationToken);

            // Pokretni prosek: (prosek * broj + nova ocena) / (broj + 1).
            rated.AverageRating = Math.Round(
                (rated.AverageRating * rated.RatingsCount + request.Score) / (rated.RatingsCount + 1), 2);
            rated.RatingsCount++;
            await _userRepository.UpdateAsync(rated, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
            return rating.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
