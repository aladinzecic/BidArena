using BidArena.Application.Auth.Commands.RefreshToken;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BidArena.Tests.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Vazeci_unos_prolazi()
    {
        var result = _validator.TestValidate(new RefreshTokenCommand("neki-refresh-token"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Prazan_token_nije_validan(string? token)
    {
        var result = _validator.TestValidate(new RefreshTokenCommand(token!));

        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
