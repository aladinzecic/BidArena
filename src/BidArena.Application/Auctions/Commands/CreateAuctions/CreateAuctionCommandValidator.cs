using BidArena.Domain.Entities;
using FluentValidation;

namespace BidArena.Application.Auctions.Commands.CreateAuction;

public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();

        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<AuctionType>(t, true, out _))
            .WithMessage("Nepoznat tip aukcije, dozvoljeno: English, Dutch");

        RuleFor(x => x.StartingPrice).GreaterThan(0);
        RuleFor(x => x.BidIncrement).GreaterThan(0);

        // English: rezervna cena >= početna. Dutch: ReservePrice je donja granica pada cene (< početna).
        RuleFor(x => x.ReservePrice)
            .GreaterThanOrEqualTo(x => x.StartingPrice)
            .When(x => x.ReservePrice.HasValue && !IsDutch(x))
            .WithMessage("Rezervna cena ne može biti manja od početne");

        RuleFor(x => x.ReservePrice)
            .NotNull().WithMessage("Za holandsku aukciju je obavezna donja granica cene")
            .GreaterThan(0)
            .LessThan(x => x.StartingPrice).WithMessage("Donja granica mora biti manja od početne cene")
            .When(IsDutch);

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Vreme kraja mora biti posle vremena početka");
    }

    private static bool IsDutch(CreateAuctionCommand command) =>
        Enum.TryParse<AuctionType>(command.Type, true, out var type) && type == AuctionType.Dutch;
}