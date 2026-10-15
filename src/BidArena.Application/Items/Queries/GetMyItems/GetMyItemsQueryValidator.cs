using FluentValidation;

namespace BidArena.Application.Items.Queries.GetMyItems;

public class GetMyItemsQueryValidator : AbstractValidator<GetMyItemsQuery>
{
    public GetMyItemsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page mora biti najmanje 1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize mora biti između 1 i 100");
    }
}
