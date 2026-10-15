using BidArena.Application.Auth.Commands.Register;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private static RegisterCommand Valid() => new("ana@example.com", "Lozinka123", "Ana", true, false);

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Bar_jedna_uloga_je_dovoljna(bool isSeller, bool isBidder)
    {
        var result = _validator.TestValidate(Valid() with { IsSeller = isSeller, IsBidder = isBidder });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Bez_ijedne_uloge_nije_validno()
    {
        var result = _validator.TestValidate(Valid() with { IsSeller = false, IsBidder = false });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Korisnik mora biti bar Seller ili Bidder");
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }

    // Email

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nije-email")]
    public void Neispravan_email_nije_validan(string? email)
    {
        var result = _validator.TestValidate(Valid() with { Email = email! });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_duzi_od_256_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { Email = new string('a', 245) + "@example.com" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // Password

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Lozinka_je_obavezna(string? password)
    {
        var result = _validator.TestValidate(Valid() with { Password = password! });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Lozinka_kraca_od_8_karaktera_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Password = "Kratka1" });

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Lozinka mora imati najmanje 8 karaktera");
    }

    [Fact]
    public void Lozinka_bez_slova_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Password = "12345678" });

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Lozinka mora sadržati bar jedno slovo");
    }

    [Fact]
    public void Lozinka_bez_cifre_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Password = "SamoSlovaBezCifre" });

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Lozinka mora sadržati bar jednu cifru");
    }

    [Fact]
    public void Lozinka_od_tacno_8_karaktera_sa_slovom_i_cifrom_je_validna()
    {
        var result = _validator.TestValidate(Valid() with { Password = "Lozinka1" });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    // DisplayName

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    public void Ime_za_prikaz_krace_od_2_karaktera_nije_validno(string? displayName)
    {
        var result = _validator.TestValidate(Valid() with { DisplayName = displayName! });

        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void Ime_za_prikaz_duze_od_100_karaktera_nije_validno()
    {
        var result = _validator.TestValidate(Valid() with { DisplayName = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(100)]
    public void Ime_za_prikaz_na_granicama_je_validno(int length)
    {
        var result = _validator.TestValidate(Valid() with { DisplayName = new string('a', length) });

        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
    }
}
