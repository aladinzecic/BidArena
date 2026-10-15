namespace BidArena.Application.Users.Dtos;

public record RatingEntryDto(int Score, string? Comment, DateTime CreatedAt);

// Javni prikaz reputacije korisnika (bez e-pošte i ostalih privatnih podataka).
public record PublicUserRatingDto(
    Guid Id,
    string DisplayName,
    bool IsVerified,
    double AverageRating,
    int RatingsCount,
    int SuccessfulAuctionsCount,
    int CancelledAuctionsCount,
    List<RatingEntryDto> RecentRatings);
