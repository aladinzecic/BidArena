namespace BidArena.Application.Admin.Queries.GetAdminStats;

public record AdminStatsDto(
    int TotalUsers,
    int VerifiedUsers,
    Dictionary<string, int> AuctionsByStatus,
    int PendingPayments,
    int OverduePayments,
    decimal TotalTurnover,
    decimal TotalCommission,
    int OpenDisputes
);
