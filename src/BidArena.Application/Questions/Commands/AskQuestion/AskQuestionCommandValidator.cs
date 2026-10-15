using FluentValidation;

namespace BidArena.Application.Questions.Commands.AskQuestion;

public class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Pitanje je obavezno")
            .MinimumLength(5).WithMessage("Pitanje mora imati najmanje 5 karaktera")
            .MaximumLength(1000).WithMessage("Pitanje može imati najviše 1000 karaktera");
    }
}
