using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Users.Queries.GetUserRating;

public record GetUserRatingQuery(Guid UserId) : IRequest<PublicUserRatingDto>;
