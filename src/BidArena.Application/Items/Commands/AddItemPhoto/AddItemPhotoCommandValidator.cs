using FluentValidation;

namespace BidArena.Application.Items.Commands.AddItemPhoto;

public class AddItemPhotoCommandValidator : AbstractValidator<AddItemPhotoCommand>
{
    public AddItemPhotoCommandValidator()
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
