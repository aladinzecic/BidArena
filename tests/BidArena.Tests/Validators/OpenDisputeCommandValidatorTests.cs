using BidArena.Application.Disputes.Commands.OpenDispute;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class OpenDisputeCommandValidatorTests
{
    private readonly OpenDisputeCommandValidator _validator = new();

    private static OpenDisputeCommand Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Predmet nije stigao u opisanom stanju");

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
    public void Razlog_je_obavezan(string? reason)
    {
        var result = _validator.TestValidate(Valid() with { Reason = reason! });

        result.ShouldHaveValidationErrorFor(x => x.Reason).WithErrorMessage("Razlog spora je obavezan");
    }

    [Fact]
    public void Razlog_kraci_od_10_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { Reason = "Kratko" });

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Razlog spora mora imati najmanje 10 karaktera");
    }

    [Fact]
    public void Razlog_duzi_od_2000_karaktera_nije_validan()
    {
        var result = _validator.TestValidate(Valid() with { Reason = new string('a', 2001) });

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(2000)]
    public void Razlog_na_granicama_je_validan(int length)
    {
        var result = _validator.TestValidate(Valid() with { Reason = new string('a', length) });

        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }
}
