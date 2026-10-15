using MediatR;

namespace BidArena.Application.Auctions.Commands.CreateAuction;

public record CreateAuctionCommand(
    Guid SellerId,
    Guid ItemId,
    string Type,
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal BidIncrement,
    DateTime StartTime,
    DateTime EndTime
) : IRequest<Guid>;