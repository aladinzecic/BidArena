using MediatR;

namespace BidArena.Application.Admin.Commands.VerifyUser;

public record VerifyUserCommand(Guid UserId) : IRequest;
