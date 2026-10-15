using BidArena.Application.Disputes.Commands.ResolveDispute;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class ResolveDisputeCommandValidatorTests
{
    private readonly ResolveDisputeCommandValidator _validator = new();

    private static ResolveDisputeCommand Valid() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Kupcu se vraća novac u celosti");

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DisputeId_je_obavezan()
    {
        var result = _validator.TestValidate(Valid() with { DisputeId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.DisputeId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Obrazlozenje_je_obavezno(string? resolution)
    {
        var result = _validator.TestValidate(Valid() with { Resolution = resolution! });

        result.ShouldHaveValidationErrorFor(x => x.Resolution)
            .WithErrorMessage("Obrazloženje odluke je obavezno");
    }

    [Fact]
    public void Obrazlozenje_krace_od_10_karaktera_nije_validno()
    {
        var result = _validator.TestValidate(Valid() with { Resolution = "Kratko" });

        result.ShouldHaveValidationErrorFor(x => x.Resolution)
            .WithErrorMessage("Obrazloženje mora imati najmanje 10 karaktera");
    }

    [Fact]
    public void Obrazlozenje_duze_od_2000_karaktera_nije_validno()
    {
        var result = _validator.TestValidate(Valid() with { Resolution = new string('a', 2001) });

        result.ShouldHaveValidationErrorFor(x => x.Resolution);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(2000)]
    public void Obrazlozenje_na_granicama_je_validno(int length)
    {
        var result = _validator.TestValidate(Valid() with { Resolution = new string('a', length) });

        result.ShouldNotHaveValidationErrorFor(x => x.Resolution);
    }
}
