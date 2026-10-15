using BidArena.Domain.Entities;
using FluentValidation;

namespace BidArena.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Naslov je obavezan")
            .Length(3, 200).WithMessage("Naslov mora imati između 3 i 200 karaktera");

        RuleFor(x => x.DescriptionMarkdown)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Opis je obavezan")
            .MaximumLength(10000).WithMessage("Opis može imati najviše 10000 karaktera");

        RuleFor(x => x.Category)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Kategorija je obavezna")
            .Must(ItemCategories.IsKnown)
            .WithMessage($"Nepoznata kategorija, dozvoljeno: {ItemCategories.AllowedList}");

        RuleFor(x => x.Condition)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Stanje predmeta je obavezno")
            .Must(c => Enum.GetNames<ItemCondition>().Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Nepoznato stanje predmeta, dozvoljeno: {string.Join(", ", Enum.GetNames<ItemCondition>())}");

        RuleFor(x => x.Location)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Lokacija je obavezna")
            .MaximumLength(200).WithMessage("Lokacija može imati najviše 200 karaktera");
    }
}
