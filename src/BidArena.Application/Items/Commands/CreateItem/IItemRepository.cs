using BidArena.Application.Common.Models;
using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IItemRepository
{
    Task<Item> AddAsync(Item item, CancellationToken cancellationToken);
    Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Item item, CancellationToken cancellationToken);
    Task DeleteAsync(Item item, CancellationToken cancellationToken);
    Task<PagedResult<Item>> GetBySellerAsync(Guid sellerId, int page, int pageSize, CancellationToken cancellationToken);
    Task AddPhotoAsync(ItemPhoto photo, CancellationToken cancellationToken);
    Task RemovePhotoAsync(ItemPhoto photo, CancellationToken cancellationToken);
}