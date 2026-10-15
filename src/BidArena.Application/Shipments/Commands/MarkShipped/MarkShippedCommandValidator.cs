using FluentValidation;

namespace BidArena.Application.Shipments.Commands.MarkShipped;

public class MarkShippedCommandValidator : AbstractValidator<MarkShippedCommand>
{
    public MarkShippedCommandValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.TrackingNumber)
            .NotEmpty().WithMessage("Broj pošiljke je obavezan")
            .MaximumLength(100);
    }
}
