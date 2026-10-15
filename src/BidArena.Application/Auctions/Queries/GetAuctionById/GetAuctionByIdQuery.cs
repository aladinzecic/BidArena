using BidArena.Application.Auctions.Dtos;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetAuctionById;

// RequesterId je null za neprijavljene korisnike.
public record GetAuctionByIdQuery(Guid AuctionId, Guid? RequesterId) : IRequest<AuctionDetailsDto>;