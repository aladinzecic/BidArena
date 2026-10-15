using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Admin.Commands.VerifyUser;

public class VerifyUserCommandHandler : IRequestHandler<VerifyUserCommand>
{
    private readonly IUserRepository _userRepository;

    public VerifyUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(VerifyUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Korisnik {request.UserId} ne postoji");

        // Idempotentno: već verifikovan korisnik nije greška.
        if (user.IsVerified)
            return;

        user.IsVerified = true;
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
