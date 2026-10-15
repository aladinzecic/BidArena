using BidArena.Application.Auth.Commands.Logout;
using BidArena.Application.Common.Interfaces;
using FluentAssertions;
using Moq;
using RefreshTokenEntity = BidArena.Domain.Entities.RefreshToken;

namespace BidArena.Tests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IJwtTokenService> _tokenService = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _tokenService.Setup(t => t.HashToken("raw-token")).Returns("hash-token");
        _handler = new LogoutCommandHandler(_refreshRepo.Object, _tokenService.Object);
    }

    private RefreshTokenEntity Stored(DateTime? revokedAt = null)
    {
        var stored = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "hash-token",
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            RevokedAt = revokedAt
        };

        _refreshRepo
            .Setup(r => r.GetByHashAsync("hash-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        return stored;
    }

    [Fact]
    public async Task Aktivan_token_se_ponistava()
    {
        var stored = Stored();

        await _handler.Handle(new LogoutCommand("raw-token"), default);

        stored.RevokedAt.Should().NotBeNull();
        _refreshRepo.Verify(r => r.UpdateAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nepoznat_token_nije_greska()
    {
        // GetByHashAsync nije podešen: Moq vraća null.
        Func<Task> act = () => _handler.Handle(new LogoutCommand("raw-token"), default);

        await act.Should().NotThrowAsync();
        _refreshRepo.Verify(r => r.UpdateAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Vec_ponisten_token_nije_greska_i_ne_menja_se()
    {
        var revokedAt = DateTime.UtcNow.AddMinutes(-10);
        var stored = Stored(revokedAt);

        Func<Task> act = () => _handler.Handle(new LogoutCommand("raw-token"), default);

        await act.Should().NotThrowAsync();
        stored.RevokedAt.Should().Be(revokedAt);
        _refreshRepo.Verify(r => r.UpdateAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dvostruki_logout_istog_tokena_je_idempotentan()
    {
        var stored = Stored();

        await _handler.Handle(new LogoutCommand("raw-token"), default);
        var firstRevokedAt = stored.RevokedAt;

        Func<Task> act = () => _handler.Handle(new LogoutCommand("raw-token"), default);

        await act.Should().NotThrowAsync();
        stored.RevokedAt.Should().Be(firstRevokedAt);
        _refreshRepo.Verify(r => r.UpdateAsync(stored, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ne_dira_ostale_tokene_korisnika()
    {
        Stored();

        await _handler.Handle(new LogoutCommand("raw-token"), default);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
