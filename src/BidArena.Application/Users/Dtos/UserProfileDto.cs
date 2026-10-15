namespace BidArena.Application.Users.Dtos;

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsSeller,
    bool IsBidder,
    bool IsAdmin,
    bool IsVerified,
    bool HasPaymentMethod,
    double AverageRating,
    int RatingsCount,
    int SuccessfulAuctionsCount,
    int CancelledAuctionsCount);
