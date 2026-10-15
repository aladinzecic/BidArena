using MediatR;

namespace BidArena.Application.Disputes.Commands.ResolveDispute;

public enum DisputeOutcome
{
    // Spor se rešava u korist prodavca: aukcija je Settled, uplata ostaje.
    FavorSeller,

    // Kupcu se vraća novac: Payment -> Refunded, aukcija -> Cancelled, prodavac dobija kazneni poen.
    RefundBuyer
}

public record ResolveDisputeCommand(
    Guid DisputeId,
    Guid AdminId,
    string Resolution,
    DisputeOutcome Outcome = DisputeOutcome.FavorSeller) : IRequest;
