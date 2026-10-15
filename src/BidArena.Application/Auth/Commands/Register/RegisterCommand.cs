using MediatR;

namespace BidArena.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    bool IsSeller,
    bool IsBidder
) : IRequest<Guid>;