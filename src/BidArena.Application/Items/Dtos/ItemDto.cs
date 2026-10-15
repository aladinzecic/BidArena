using BidArena.Domain.Entities;

namespace BidArena.Application.Items.Dtos;

public record ItemPhotoDto(Guid Id, string Url, int OrderIndex);

public record ItemDto(
    Guid Id,
    Guid SellerId,
    string Title,
    string DescriptionMarkdown,
    string Category,
    string Condition,
    string Location,
    DateTime CreatedAt,
    List<ItemPhotoDto> Photos)
{
    public static ItemDto From(Item item) => new(
        item.Id,
        item.SellerId,
        item.Title,
        item.DescriptionMarkdown,
        item.Category.ToString(),
        item.Condition.ToString(),
        item.Location,
        item.CreatedAt,
        item.Photos
            .OrderBy(p => p.OrderIndex)
            .Select(p => new ItemPhotoDto(p.Id, p.Url, p.OrderIndex))
            .ToList());
}
