using BidArena.Application.Auctions.Commands.CreateAuction;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class CreateAuctionCommandValidatorTests
{
    private readonly CreateAuctionCommandValidator _validator = new();

    private static CreateAuctionCommand Valid()
    {
        var start = DateTime.UtcNow.AddHours(1);
        return new CreateAuctionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "English", 100m, 150m, 5m, start, start.AddDays(3));
    }

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Vazeci_unos_bez_rezervne_cene_prolazi()
    {
        var result = _validator.TestValidate(Valid() with { ReservePrice = null });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ItemId_je_obavezan()
    {
        var result = _validator.TestValidate(Valid() with { ItemId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.ItemId);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Dutch")]
    [InlineData("english")]
    [InlineData("DUTCH")]
    public void Poznat_tip_aukcije_je_validan(string type)
    {
        var result = _validator.TestValidate(Valid() with { Type = type });

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("Sealed")]
    [InlineData("")]
    [InlineData(null)]
    public void Nepoznat_tip_aukcije_nije_validan(string? type)
    {
        var result = _validator.TestValidate(Valid() with { Type = type! });

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage("Nepoznat tip aukcije, dozvoljeno: English, Dutch");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Pocetna_cena_mora_biti_veca_od_nule(int startingPrice)
    {
        var result = _validator.TestValidate(Valid() with { StartingPrice = (decimal)startingPrice });

        result.ShouldHaveValidationErrorFor(x => x.StartingPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Korak_ponude_mora_biti_veci_od_nule(int bidIncrement)
    {
        var result = _validator.TestValidate(Valid() with { BidIncrement = (decimal)bidIncrement });

        result.ShouldHaveValidationErrorFor(x => x.BidIncrement);
    }

    [Fact]
    public void Rezervna_cena_manja_od_pocetne_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { StartingPrice = 100m, ReservePrice = 99m });

        result.ShouldHaveValidationErrorFor(x => x.ReservePrice)
            .WithErrorMessage("Rezervna cena ne može biti manja od početne");
    }

    [Fact]
    public void Rezervna_cena_jednaka_pocetnoj_je_validna()
    {
        var result = _validator.TestValidate(Valid() with { StartingPrice = 100m, ReservePrice = 100m });

        result.ShouldNotHaveValidationErrorFor(x => x.ReservePrice);
    }

    [Fact]
    public void Kraj_jednak_pocetku_nije_validan()
    {
        var start = DateTime.UtcNow.AddHours(1);

        var result = _validator.TestValidate(Valid() with { StartTime = start, EndTime = start });

        result.ShouldHaveValidationErrorFor(x => x.EndTime)
            .WithErrorMessage("Vreme kraja mora biti posle vremena početka");
    }

    [Fact]
    public void Kraj_pre_pocetka_nije_validan()
    {
        var start = DateTime.UtcNow.AddHours(1);

        var result = _validator.TestValidate(Valid() with { StartTime = start, EndTime = start.AddMinutes(-1) });

        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }
}
