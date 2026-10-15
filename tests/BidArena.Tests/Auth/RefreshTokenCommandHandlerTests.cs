using BidArena.Application.Auth.Commands.RefreshToken;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;
using RefreshTokenEntity = BidArena.Domain.Entities.RefreshToken;

namespace BidArena.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private static readonly DateTime NewAccessExpiry = DateTime.UtcNow.AddMinutes(15);
    private static readonly DateTime NewRefreshExpiry = DateTime.UtcNow.AddDays(7);

    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IJwtTokenService> _tokenService = new();
    private readonly List<RefreshTokenEntity> _addedTokens = new();
    private readonly RefreshTokenCommandHandler _handler;

    private readonly User _user = new() { Id = Guid.NewGuid(), Email = "ana@example.com", DisplayName = "Ana" };

    public RefreshTokenCommandHandlerTests()
    {
        _tokenService.Setup(t => t.HashToken("raw-old")).Returns("hash-old");
        _tokenService.Setup(t => t.GenerateAccessToken(_user)).Returns(new AccessToken("new-access", NewAccessExpiry));
        _tokenService.Setup(t => t.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw-new", "hash-new", NewRefreshExpiry));

        _userRepo
            .Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);

        _refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshTokenEntity, CancellationToken>((t, _) => _addedTokens.Add(t))
            .Returns(Task.CompletedTask);

        _handler = new RefreshTokenCommandHandler(_refreshRepo.Object, _userRepo.Object, _tokenService.Object);
    }

    private RefreshTokenEntity StoredToken(
        DateTime? expiresAt = null, DateTime? revokedAt = null, string? replacedBy = null)
    {
        var stored = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = _user.Id,
            TokenHash = "hash-old",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(6),
            RevokedAt = revokedAt,
            ReplacedByTokenHash = replacedBy
        };

        _refreshRepo
            .Setup(r => r.GetByHashAsync("hash-old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        return stored;
    }

    [Fact]
    public async Task Rotacija_ponistava_stari_token_i_belezi_ReplacedByTokenHash()
    {
        var stored = StoredToken();

        var response = await _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        stored.RevokedAt.Should().NotBeNull();
        stored.ReplacedByTokenHash.Should().Be("hash-new");

        var added = _addedTokens.Should().ContainSingle().Subject;
        added.UserId.Should().Be(_user.Id);
        added.TokenHash.Should().Be("hash-new");
        added.ExpiresAt.Should().Be(NewRefreshExpiry);
        added.RevokedAt.Should().BeNull();

        response.AccessToken.Should().Be("new-access");
        response.RefreshToken.Should().Be("raw-new");
        response.RefreshTokenExpiresAt.Should().Be(NewRefreshExpiry);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ponovna_upotreba_rotiranog_tokena_ponistava_sve_tokene_korisnika()
    {
        StoredToken(revokedAt: DateTime.UtcNow.AddMinutes(-5), replacedBy: "hash-newer");

        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>();
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(_user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Istekao_token_baca_UnauthorizedAccessException_bez_izdavanja_novog()
    {
        StoredToken(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>().WithMessage("Refresh token je istekao");
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Token_ponisten_logout_om_ne_dira_ostale_tokene_korisnika()
    {
        // Logout postavlja samo RevokedAt, bez ReplacedByTokenHash: to nije znak krađe.
        StoredToken(revokedAt: DateTime.UtcNow.AddMinutes(-5), replacedBy: null);

        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>();
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Nepoznat_token_baca_UnauthorizedAccessException()
    {
        // GetByHashAsync nije podešen: Moq vraća null.
        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>().WithMessage("Nevažeći refresh token");
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Korisnik_koji_vise_ne_postoji_ne_dobija_nove_tokene()
    {
        var stored = StoredToken();
        _userRepo
            .Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("raw-old"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>();
        stored.RevokedAt.Should().BeNull();
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
