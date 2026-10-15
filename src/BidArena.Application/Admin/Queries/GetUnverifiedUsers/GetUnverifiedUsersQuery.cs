using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Admin.Queries.GetUnverifiedUsers;

public record GetUnverifiedUsersQuery : IRequest<List<UserProfileDto>>;
