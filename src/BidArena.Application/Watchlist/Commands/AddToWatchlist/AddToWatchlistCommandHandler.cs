using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Watchlist.Commands.AddToWatchlist;

public class AddToWatchlistCommandHandler : IRequestHandler<AddToWatchlistCommand>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IAuctionRepository _auctionRepository;

    public AddToWatchlistCommandHandler(
        IWatchlistRepository watchlistRepository,
        IAuctionRepository auctionRepository)
    {
        _watchlistRepository = watchlistRepository;
        _auctionRepository = auctionRepository;
    }

    public async Task Handle(AddToWatchlistCommand request, CancellationToken cancellationToken)
    {
        var exists = await _auctionRepository.ExistsAsync(request.AuctionId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        var alreadyWatching = await _watchlistRepository.ExistsAsync(
            request.UserId, request.AuctionId, cancellationToken);

        if (alreadyWatching)
            return; // idempotentno - ponovni zahtev ne baca gresku

        await _watchlistRepository.AddAsync(
            new WatchlistItem
            {
                UserId = request.UserId,
                AuctionId = request.AuctionId,
                AddedAt = DateTime.UtcNow
            },
            cancellationToken);
    }
}
