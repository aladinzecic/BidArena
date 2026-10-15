using FluentValidation;

namespace BidArena.Application.Disputes.Commands.ResolveDispute;

public class ResolveDisputeCommandValidator : AbstractValidator<ResolveDisputeCommand>
{
    public ResolveDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty();

        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("Obrazloženje odluke je obavezno")
            .MinimumLength(10).WithMessage("Obrazloženje mora imati najmanje 10 karaktera")
            .MaximumLength(2000);
    }
}
