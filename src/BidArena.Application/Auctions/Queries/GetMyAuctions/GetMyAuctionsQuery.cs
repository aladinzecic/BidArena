using BidArena.Application.Auctions.Dtos;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetMyAuctions;

public record GetMyAuctionsQuery(Guid SellerId) : IRequest<List<AuctionListItemDto>>;
