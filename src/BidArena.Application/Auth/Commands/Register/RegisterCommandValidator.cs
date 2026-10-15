using FluentValidation;

namespace BidArena.Application.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Lozinka mora imati najmanje 8 karaktera")
            .Matches("[A-Za-z]").WithMessage("Lozinka mora sadržati bar jedno slovo")
            .Matches("[0-9]").WithMessage("Lozinka mora sadržati bar jednu cifru");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x)
            .Must(x => x.IsSeller || x.IsBidder)
            .WithName("Role")
            .WithMessage("Korisnik mora biti bar Seller ili Bidder");
    }
}