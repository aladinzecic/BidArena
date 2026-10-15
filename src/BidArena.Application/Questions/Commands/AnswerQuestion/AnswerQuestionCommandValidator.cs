using FluentValidation;

namespace BidArena.Application.Questions.Commands.AnswerQuestion;

public class AnswerQuestionCommandValidator : AbstractValidator<AnswerQuestionCommand>
{
    public AnswerQuestionCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.QuestionId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Odgovor je obavezan")
            .MaximumLength(2000).WithMessage("Odgovor može imati najviše 2000 karaktera");
    }
}
