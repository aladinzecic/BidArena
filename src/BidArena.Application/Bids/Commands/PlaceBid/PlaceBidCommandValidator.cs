using FluentValidation;

namespace BidArena.Application.Bids.Commands.PlaceBid;

public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.BidderId).NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Iznos ponude mora biti veći od 0");

        RuleFor(x => x.ProxyMaxAmount)
            .GreaterThanOrEqualTo(x => x.Amount)
            .When(x => x.ProxyMaxAmount.HasValue)
            .WithMessage("Maksimalna proxy ponuda ne može biti manja od iznosa ponude");
    }
}