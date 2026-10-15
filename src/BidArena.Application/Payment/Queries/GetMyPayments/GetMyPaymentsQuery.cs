using MediatR;

namespace BidArena.Application.Payments.Queries.GetMyPayments;

public record GetMyPaymentsQuery(Guid UserId) : IRequest<List<PaymentDto>>;