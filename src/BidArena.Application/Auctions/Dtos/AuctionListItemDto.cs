namespace BidArena.Application.Auctions.Dtos;

public class AuctionListItemDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? MainPhotoUrl { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal BidIncrement { get; set; }
    public int BidCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}