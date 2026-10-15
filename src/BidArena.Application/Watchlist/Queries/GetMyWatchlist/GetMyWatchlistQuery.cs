using BidArena.Application.Auctions.Dtos;
using MediatR;

namespace BidArena.Application.Watchlist.Queries.GetMyWatchlist;

public record GetMyWatchlistQuery(Guid UserId) : IRequest<List<AuctionListItemDto>>;
