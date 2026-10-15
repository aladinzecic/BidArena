using MediatR;

namespace BidArena.Application.Watchlist.Commands.RemoveFromWatchlist;

public record RemoveFromWatchlistCommand(Guid UserId, Guid AuctionId) : IRequest;
