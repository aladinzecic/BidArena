using BidArena.Application.Shipments.Commands.MarkShipped;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class MarkShippedCommandValidatorTests
{
    private readonly MarkShippedCommandValidator _validator = new();

    private static MarkShippedCommand Valid() => new(Guid.NewGuid(), Guid.NewGuid(), "RS123456789");

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AuctionId_je_obavezan()
    {
        var result = _validator.TestValidate(Valid() with { AuctionId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.AuctionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Broj_posiljke_je_obavezan(string? trackingNumber)
    {
        var result = _validator.TestValidate(Valid() with { TrackingNumber = trackingNumber! });

        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber).WithErrorMessage("Broj pošiljke je obavezan");
    }

    [Fact]
    public void Broj_posiljke_duzi_od_100_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { TrackingNumber = new string('1', 101) });

        result.ShouldHaveValidationErrorFor(x => x.TrackingNumber);
    }

    [Fact]
    public void Broj_posiljke_od_tacno_100_karaktera_je_validan()
    {
        var result = _validator.TestValidate(Valid() with { TrackingNumber = new string('1', 100) });

        result.ShouldNotHaveValidationErrorFor(x => x.TrackingNumber);
    }
}
