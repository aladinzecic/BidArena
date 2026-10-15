using BidArena.Domain.Entities;

namespace BidArena.Api.Middleware;

/// <summary>
/// Deklariše u kojim fazama aukcije je endpoint dozvoljen. Čita je <see cref="AuctionPhaseMiddleware"/>
/// iz metapodataka endpoint-a, pre nego što se zahtev prosledi kontroleru/handler-u.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class AllowedPhasesAttribute : Attribute
{
    public AuctionStatus[] Phases { get; }

    public AllowedPhasesAttribute(params AuctionStatus[] phases)
    {
        Phases = phases;
    }
}
