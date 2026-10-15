using BidArena.Application.Auth.Commands.Login;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auth;

public class LoginCommandHandlerTests
{
    private const string GenericMessage = "Pogrešan email ili lozinka";

    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly List<RefreshToken> _savedTokens = new();
    private readonly LoginCommandHandler _handler;

    private static readonly DateTime AccessExpiry = DateTime.UtcNow.AddMinutes(15);
    private static readonly DateTime RefreshExpiry = DateTime.UtcNow.AddDays(7);

    public LoginCommandHandlerTests()
    {
        _refreshRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => _savedTokens.Add(t))
            .Returns(Task.CompletedTask);

        _handler = new LoginCommandHandler(
            _userRepo.Object, _hasher.Object, _tokenService.Object, _refreshRepo.Object);
    }

    private User ExistingUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "ana@example.com",
            PasswordHash = "STORED_HASH",
            DisplayName = "Ana"
        };

        _userRepo
            .Setup(r => r.GetByEmailAsync("ana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return user;
    }

    [Fact]
    public async Task Pogresna_lozinka_baca_UnauthorizedAccessException()
    {
        ExistingUser();
        _hasher.Setup(h => h.Verify("pogresna", "STORED_HASH")).Returns(false);

        Func<Task> act = () => _handler.Handle(new LoginCommand("ana@example.com", "pogresna"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>().WithMessage(GenericMessage);
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Nepostojeci_email_baca_isti_izuzetak_kao_pogresna_lozinka()
    {
        // GetByEmailAsync nije podešen: Moq vraća null (korisnik ne postoji).
        Func<Task> act = () => _handler.Handle(new LoginCommand("niko@example.com", "bilo_sta"), default);

        await act.Should().ThrowExactlyAsync<UnauthorizedAccessException>().WithMessage(GenericMessage);
        _refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Uspesna_prijava_pravi_refresh_token_i_vraca_tokene()
    {
        var user = ExistingUser();
        _hasher.Setup(h => h.Verify("Lozinka123", "STORED_HASH")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns(new AccessToken("access-jwt", AccessExpiry));
        _tokenService.Setup(t => t.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw-refresh", "hash-refresh", RefreshExpiry));

        var response = await _handler.Handle(new LoginCommand("ana@example.com", "Lozinka123"), default);

        response.AccessToken.Should().Be("access-jwt");
        response.AccessTokenExpiresAt.Should().Be(AccessExpiry);
        response.RefreshToken.Should().Be("raw-refresh");
        response.RefreshTokenExpiresAt.Should().Be(RefreshExpiry);

        var saved = _savedTokens.Should().ContainSingle().Subject;
        saved.UserId.Should().Be(user.Id);
        saved.TokenHash.Should().Be("hash-refresh");
        saved.ExpiresAt.Should().Be(RefreshExpiry);
        saved.RevokedAt.Should().BeNull();
        saved.ReplacedByTokenHash.Should().BeNull();
    }

    [Fact]
    public async Task U_bazu_se_cuva_samo_hes_a_ne_sam_refresh_token()
    {
        ExistingUser();
        _hasher.Setup(h => h.Verify("Lozinka123", "STORED_HASH")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns(new AccessToken("a", AccessExpiry));
        _tokenService.Setup(t => t.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw-refresh", "hash-refresh", RefreshExpiry));

        await _handler.Handle(new LoginCommand("ana@example.com", "Lozinka123"), default);

        _savedTokens.Single().TokenHash.Should().NotBe("raw-refresh");
    }

    [Fact]
    public async Task Email_se_normalizuje_pre_trazenja_korisnika()
    {
        var user = ExistingUser();
        _hasher.Setup(h => h.Verify("Lozinka123", "STORED_HASH")).Returns(true);
        _tokenService.Setup(t => t.GenerateAccessToken(user)).Returns(new AccessToken("a", AccessExpiry));
        _tokenService.Setup(t => t.GenerateRefreshToken())
            .Returns(new GeneratedRefreshToken("raw", "hash", RefreshExpiry));

        var response = await _handler.Handle(new LoginCommand("  ANA@Example.com ", "Lozinka123"), default);

        response.AccessToken.Should().Be("a");
    }
}
