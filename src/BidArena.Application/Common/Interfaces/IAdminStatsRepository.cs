using BidArena.Application.Admin.Queries.GetAdminStats;

namespace BidArena.Application.Common.Interfaces;

public interface IAdminStatsRepository
{
    Task<AdminStatsDto> GetAsync(CancellationToken cancellationToken);
}
