using BidArena.Domain.Entities;

namespace BidArena.Application.Disputes.Dtos;

public record DisputePhotoDto(Guid Id, string Url, int OrderIndex);

public record DisputeDto(
    Guid Id,
    Guid AuctionId,
    Guid OpenedByUserId,
    string Reason,
    string Status,
    string? Resolution,
    Guid? ResolvedByAdminId,
    DateTime CreatedAt,
    List<DisputePhotoDto> Photos
)
{
    public static DisputeDto From(Dispute d) => new(
        d.Id,
        d.AuctionId,
        d.OpenedByUserId,
        d.Reason,
        d.Status.ToString(),
        d.Resolution,
        d.ResolvedByAdminId,
        d.CreatedAt,
        d.Photos
            .OrderBy(p => p.OrderIndex)
            .Select(p => new DisputePhotoDto(p.Id, p.Url, p.OrderIndex))
            .ToList());
}
