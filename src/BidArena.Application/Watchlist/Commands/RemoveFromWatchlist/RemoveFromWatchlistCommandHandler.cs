using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Watchlist.Commands.RemoveFromWatchlist;

public class RemoveFromWatchlistCommandHandler : IRequestHandler<RemoveFromWatchlistCommand>
{
    private readonly IWatchlistRepository _watchlistRepository;

    public RemoveFromWatchlistCommandHandler(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository;
    }

    public async Task Handle(RemoveFromWatchlistCommand request, CancellationToken cancellationToken)
    {
        await _watchlistRepository.RemoveAsync(request.UserId, request.AuctionId, cancellationToken);
    }
}
