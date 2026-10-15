using FluentValidation;

namespace BidArena.Application.Disputes.Commands.OpenDispute;

public class OpenDisputeCommandValidator : AbstractValidator<OpenDisputeCommand>
{
    public OpenDisputeCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Razlog spora je obavezan")
            .MinimumLength(10).WithMessage("Razlog spora mora imati najmanje 10 karaktera")
            .MaximumLength(2000);
    }
}
