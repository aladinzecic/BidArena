using BidArena.Application.Common.Interfaces;
using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Admin.Queries.GetUnverifiedUsers;

public class GetUnverifiedUsersQueryHandler : IRequestHandler<GetUnverifiedUsersQuery, List<UserProfileDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUnverifiedUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserProfileDto>> Handle(GetUnverifiedUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetUnverifiedAsync(cancellationToken);

        return users
            .Select(u => new UserProfileDto(
                u.Id, u.Email, u.DisplayName, u.IsSeller, u.IsBidder, u.IsAdmin, u.IsVerified,
                u.HasPaymentMethod, u.AverageRating, u.RatingsCount,
                u.SuccessfulAuctionsCount, u.CancelledAuctionsCount))
            .ToList();
    }
}
