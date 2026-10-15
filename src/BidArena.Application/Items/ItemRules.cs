using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;

namespace BidArena.Application.Items;

internal static class ItemRules
{
    public const int MaxPhotosPerItem = 10;

    // Predmet se sme menjati samo dok nema aukciju koja je (bila) vidljiva kupcima:
    // dozvoljeni su samo Draft i Cancelled.
    public static async Task EnsureEditableAsync(
        IAuctionRepository auctionRepository, Guid itemId, CancellationToken cancellationToken)
    {
        var statuses = await auctionRepository.GetStatusesByItemIdAsync(itemId, cancellationToken);

        if (statuses.Any(s => s != AuctionStatus.Draft && s != AuctionStatus.Cancelled))
            throw new InvalidOperationException(
                "Predmet se ne može menjati jer ima zakazanu, aktivnu ili završenu aukciju");
    }
}
