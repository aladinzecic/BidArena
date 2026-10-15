using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using BidArena.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BidArena.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<List<User>> GetUnverifiedAsync(CancellationToken cancellationToken)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsVerified && !u.IsAdmin)
            .OrderBy(u => u.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        // Entitet je tracked, EF sam detektuje izmene.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementCancelledAuctionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Jedan UPDATE ... SET x = x + 1, pa istovremena otkazivanja ne gube brojac.
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.CancelledAuctionsCount, u => u.CancelledAuctionsCount + 1),
                cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }
}
