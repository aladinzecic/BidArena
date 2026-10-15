using MediatR;

namespace BidArena.Application.Users.Commands.AddPaymentMethod;

// Simulacija: u realnom sistemu ovde bi se registrovala kartica kod payment provajdera.
public record AddPaymentMethodCommand(Guid UserId) : IRequest;
