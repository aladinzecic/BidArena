using BidArena.Application.Common.Interfaces;
using BidArena.Application.Users.Dtos;
using MediatR;

namespace BidArena.Application.Users.Queries.GetUserRating;

public class GetUserRatingQueryHandler : IRequestHandler<GetUserRatingQuery, PublicUserRatingDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IRatingRepository _ratingRepository;

    public GetUserRatingQueryHandler(IUserRepository userRepository, IRatingRepository ratingRepository)
    {
        _userRepository = userRepository;
        _ratingRepository = ratingRepository;
    }

    public async Task<PublicUserRatingDto> Handle(GetUserRatingQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Korisnik {request.UserId} ne postoji");

        var ratings = await _ratingRepository.GetForUserAsync(user.Id, cancellationToken);

        // Ko je dao ocenu se namerno ne otkriva, vide se samo ocena, komentar i datum.
        return new PublicUserRatingDto(
            user.Id,
            user.DisplayName,
            user.IsVerified,
            user.AverageRating,
            user.RatingsCount,
            user.SuccessfulAuctionsCount,
            user.CancelledAuctionsCount,
            ratings.Select(r => new RatingEntryDto(r.Score, r.Comment, r.CreatedAt)).ToList());
    }
}
