using MediatR;

namespace BidArena.Application.Payments.Commands.MarkPaymentOverdue;

public record MarkPaymentOverdueCommand(Guid PaymentId) : IRequest;