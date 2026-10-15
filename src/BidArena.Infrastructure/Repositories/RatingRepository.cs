using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly ApplicationDbContext _context;

    public RatingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Rating rating, CancellationToken cancellationToken)
    {
        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid auctionId, Guid raterId, CancellationToken cancellationToken)
    {
        return await _context.Ratings
            .AnyAsync(r => r.AuctionId == auctionId && r.RaterId == raterId, cancellationToken);
    }

    public async Task<List<Rating>> GetForUserAsync(Guid ratedUserId, CancellationToken cancellationToken)
    {
        return await _context.Ratings
            .AsNoTracking()
            .Where(r => r.RatedUserId == ratedUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
    }
}
