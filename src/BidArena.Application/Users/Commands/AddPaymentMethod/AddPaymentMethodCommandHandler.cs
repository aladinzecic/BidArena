using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Users.Commands.AddPaymentMethod;

public class AddPaymentMethodCommandHandler : IRequestHandler<AddPaymentMethodCommand>
{
    private readonly IUserRepository _userRepository;

    public AddPaymentMethodCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Korisnik {request.UserId} ne postoji");

        user.HasPaymentMethod = true;
        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}
