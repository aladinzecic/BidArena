using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Users.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid UserId) : IRequest<UserProfileDto>;
