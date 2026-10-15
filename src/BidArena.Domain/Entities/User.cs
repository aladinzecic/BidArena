namespace BidArena.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public bool IsSeller { get; set; }
    public bool IsBidder { get; set; }
    public bool IsAdmin { get; set; }

    public bool IsVerified { get; set; }
    public bool HasPaymentMethod { get; set; }

    public double AverageRating { get; set; }
    public int RatingsCount { get; set; }
    public int SuccessfulAuctionsCount { get; set; }
    public int CancelledAuctionsCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}