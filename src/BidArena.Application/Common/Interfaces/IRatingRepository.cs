using BidArena.Domain.Entities;

namespace BidArena.Application.Common.Interfaces;

public interface IRatingRepository
{
    Task AddAsync(Rating rating, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid auctionId, Guid raterId, CancellationToken cancellationToken);
    Task<List<Rating>> GetForUserAsync(Guid ratedUserId, CancellationToken cancellationToken);
}
