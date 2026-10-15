using MediatR;

namespace BidArena.Application.Ratings.Commands.CreateRating;

public record CreateRatingCommand(Guid AuctionId, Guid RaterId, int Score, string? Comment) : IRequest<Guid>;
