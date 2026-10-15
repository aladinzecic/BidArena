using MediatR;

namespace BidArena.Application.Admin.Queries.GetAdminStats;

public record GetAdminStatsQuery : IRequest<AdminStatsDto>;
