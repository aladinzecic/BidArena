using BidArena.Application.Auth.Commands.Register;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        _hasher.Setup(h => h.Hash("Lozinka123")).Returns("HASHED");

        _handler = new RegisterCommandHandler(_userRepo.Object, _hasher.Object);
    }

    [Fact]
    public async Task Duplikat_emaila_baca_InvalidOperationException()
    {
        _userRepo
            .Setup(r => r.EmailExistsAsync("ana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterCommand("ana@example.com", "Lozinka123", "Ana", true, false);

        Func<Task> act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Duplikat_se_proverava_nad_normalizovanim_emailom()
    {
        // Postoji samo "ana@example.com"; unos sa razmacima i velikim slovima mora da pogodi isti nalog.
        _userRepo
            .Setup(r => r.EmailExistsAsync("ana@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new RegisterCommand("  Ana@Example.COM ", "Lozinka123", "Ana", true, false);

        Func<Task> act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Email_se_cuva_u_malim_slovima_bez_razmaka()
    {
        User? saved = null;
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        var command = new RegisterCommand("  Ana@Example.COM ", "Lozinka123", "  Ana Anić ", true, true);

        var id = await _handler.Handle(command, default);

        saved.Should().NotBeNull();
        saved!.Email.Should().Be("ana@example.com");
        saved.DisplayName.Should().Be("Ana Anić");
        saved.IsSeller.Should().BeTrue();
        saved.IsBidder.Should().BeTrue();
        saved.IsAdmin.Should().BeFalse();
        id.Should().Be(saved.Id);
    }

    [Fact]
    public async Task Lozinka_se_cuva_samo_kao_hes()
    {
        User? saved = null;
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        await _handler.Handle(new RegisterCommand("ana@example.com", "Lozinka123", "Ana", true, false), default);

        saved!.PasswordHash.Should().Be("HASHED");
        saved.PasswordHash.Should().NotBe("Lozinka123");
    }
}
