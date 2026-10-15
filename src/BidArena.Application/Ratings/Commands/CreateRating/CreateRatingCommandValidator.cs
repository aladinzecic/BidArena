using FluentValidation;

namespace BidArena.Application.Ratings.Commands.CreateRating;

public class CreateRatingCommandValidator : AbstractValidator<CreateRatingCommand>
{
    public CreateRatingCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 5).WithMessage("Ocena mora biti između 1 i 5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000)
            .When(x => x.Comment is not null);
    }
}
