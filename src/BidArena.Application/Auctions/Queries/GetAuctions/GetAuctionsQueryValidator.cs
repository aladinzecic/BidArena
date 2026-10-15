using BidArena.Application.Items;
using BidArena.Domain.Entities;
using FluentValidation;

namespace BidArena.Application.Auctions.Queries.GetAuctions;

public class GetAuctionsQueryValidator : AbstractValidator<GetAuctionsQuery>
{
    public GetAuctionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);

        RuleFor(x => x.Status)
            .NotEqual(AuctionStatus.Draft)
            .WithMessage("Draft aukcije nisu javne");

        RuleFor(x => x.Search).MaximumLength(100);
        // Kategorija je opciona, ali ako je zadata mora biti jedna od vrednosti enuma ItemCategory.
        RuleFor(x => x.Category)
            .Must(ItemCategories.IsKnown)
            .When(x => !string.IsNullOrWhiteSpace(x.Category))
            .WithMessage($"Nepoznata kategorija, dozvoljeno: {ItemCategories.AllowedList}");
    }
}