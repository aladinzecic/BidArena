using BidArena.Application.Bids.Commands.PlaceBid;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class PlaceBidCommandValidatorTests
{
    private readonly PlaceBidCommandValidator _validator = new();

    private static PlaceBidCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid(), 100m, null);

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Vazeci_unos_sa_proxy_ponudom_prolazi()
    {
        var result = _validator.TestValidate(Valid() with { ProxyMaxAmount = 200m });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AuctionId_je_obavezan()
    {
        var result = _validator.TestValidate(Valid() with { AuctionId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.AuctionId);
    }

    [Fact]
    public void BidderId_je_obavezan()
    {
        var result = _validator.TestValidate(Valid() with { BidderId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.BidderId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Iznos_mora_biti_veci_od_nule(int amount)
    {
        var result = _validator.TestValidate(Valid() with { Amount = amount });

        result.ShouldHaveValidationErrorFor(x => x.Amount).WithErrorMessage("Iznos ponude mora biti veći od 0");
    }

    [Fact]
    public void Proxy_manja_od_iznosa_nije_validna()
    {
        var result = _validator.TestValidate(Valid() with { Amount = 100m, ProxyMaxAmount = 99m });

        result.ShouldHaveValidationErrorFor(x => x.ProxyMaxAmount)
            .WithErrorMessage("Maksimalna proxy ponuda ne može biti manja od iznosa ponude");
    }

    [Fact]
    public void Proxy_jednaka_iznosu_je_validna()
    {
        var result = _validator.TestValidate(Valid() with { Amount = 100m, ProxyMaxAmount = 100m });

        result.ShouldNotHaveValidationErrorFor(x => x.ProxyMaxAmount);
    }
}
