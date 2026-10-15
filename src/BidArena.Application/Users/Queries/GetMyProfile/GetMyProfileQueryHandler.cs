using BidArena.Application.Common.Interfaces;
using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Users.Queries.GetMyProfile;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    private readonly IUserRepository _userRepository;

    public GetMyProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var u = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Korisnik {request.UserId} ne postoji");

        return new UserProfileDto(
            u.Id, u.Email, u.DisplayName, u.IsSeller, u.IsBidder, u.IsAdmin, u.IsVerified,
            u.HasPaymentMethod, u.AverageRating, u.RatingsCount,
            u.SuccessfulAuctionsCount, u.CancelledAuctionsCount);
    }
}
