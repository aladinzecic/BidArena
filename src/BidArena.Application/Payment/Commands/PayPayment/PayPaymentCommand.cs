using MediatR;

namespace BidArena.Application.Payments.Commands.PayPayment;

public record PayPaymentCommand(Guid PaymentId, Guid UserId) : IRequest;