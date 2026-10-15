using MediatR;

namespace BidArena.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;