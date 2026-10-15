using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<List<User>> GetUnverifiedAsync(CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);

    // Atomarno povecava CancelledAuctionsCount (bez ucitavanja entiteta, bez race condition-a).
    Task IncrementCancelledAuctionsAsync(Guid userId, CancellationToken cancellationToken);
}