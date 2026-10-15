using MediatR;

namespace BidArena.Application.Watchlist.Commands.AddToWatchlist;

public record AddToWatchlistCommand(Guid UserId, Guid AuctionId) : IRequest;
