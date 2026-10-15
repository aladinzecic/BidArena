using BidArena.Application.Common.Interfaces;
using BidArena.Application.Common.Models;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly ApplicationDbContext _context;

    public ItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Item> AddAsync(Item item, CancellationToken cancellationToken)
    {
        _context.Items.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Item?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Items
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Item item, CancellationToken cancellationToken)
    {
        // Entitet je tracked, EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Item item, CancellationToken cancellationToken)
    {
        // Slike se brisu kaskadno (obavezni FK).
        _context.Items.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<Item>> GetBySellerAsync(
        Guid sellerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Items
            .AsNoTracking()
            .Where(i => i.SellerId == sellerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(i => i.Photos)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Item>(items, totalCount, page, pageSize);
    }

    public async Task AddPhotoAsync(ItemPhoto photo, CancellationToken cancellationToken)
    {
        _context.ItemPhotos.Add(photo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemovePhotoAsync(ItemPhoto photo, CancellationToken cancellationToken)
    {
        _context.ItemPhotos.Remove(photo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}