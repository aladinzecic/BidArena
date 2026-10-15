using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Admin.Queries.GetAdminStats;

public class GetAdminStatsQueryHandler : IRequestHandler<GetAdminStatsQuery, AdminStatsDto>
{
    private readonly IAdminStatsRepository _statsRepository;

    public GetAdminStatsQueryHandler(IAdminStatsRepository statsRepository)
    {
        _statsRepository = statsRepository;
    }

    public Task<AdminStatsDto> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
    {
        return _statsRepository.GetAsync(cancellationToken);
    }
}
