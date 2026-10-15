using BidArena.Application.Items.Commands.AddItemPhoto;
using FluentValidation;

namespace BidArena.Application.Disputes.Commands.AddDisputePhoto;

// Ista pravila kao za slike predmeta: JPEG, PNG ili WebP do 5 MB.
public class AddDisputePhotoCommandValidator : AbstractValidator<AddDisputePhotoCommand>
{
    public AddDisputePhotoCommandValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(ct => !string.IsNullOrEmpty(ct) && AddItemPhotoCommand.AllowedContentTypes.ContainsKey(ct))
            .WithMessage("Dozvoljeni formati slike su: JPEG, PNG i WebP");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("Fajl je prazan")
            .LessThanOrEqualTo(AddItemPhotoCommand.MaxSizeBytes)
            .WithMessage("Slika može imati najviše 5 MB");
    }
}
