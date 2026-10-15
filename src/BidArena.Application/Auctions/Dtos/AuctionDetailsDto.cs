namespace BidArena.Application.Auctions.Dtos;

public class ItemPhotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public class AuctionDetailsDto
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }

    // Predmet
    public Guid ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DescriptionMarkdown { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<ItemPhotoDto> Photos { get; set; } = new();

    // Aukcija
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal BidIncrement { get; set; }
    public decimal MinimumNextBid { get; set; }
    public Guid? CurrentWinnerId { get; set; }
    public bool HasReservePrice { get; set; }
    public bool ReserveMet { get; set; }
    public int BidCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int AntiSnipeWindowMinutes { get; set; }
    public int AntiSnipeExtensionMinutes { get; set; }
}