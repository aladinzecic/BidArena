using BidArena.Domain.Entities;

namespace BidArena.Application.Auctions.Models;

public record AuctionWithItem(Auction Auction, Item Item, int BidCount, string? MainPhotoUrl);

public record AuctionSearchCriteria(
    AuctionStatus Status,
    ItemCategory? Category,
    string? Search,
    int Page,
    int PageSize);