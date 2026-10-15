using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Commands.AddDisputePhoto;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Disputes;

public class DisputePhotoTests
{
    private static readonly Guid Opener = Guid.NewGuid();
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

    private readonly Mock<IDisputeRepository> _disputeRepo = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly List<DisputePhoto> _saved = new();
    private readonly AddDisputePhotoCommandHandler _handler;

    public DisputePhotoTests()
    {
        _storage
            .Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/disputes/x/photo.png");

        _disputeRepo
            .Setup(r => r.AddPhotoAsync(It.IsAny<DisputePhoto>(), It.IsAny<CancellationToken>()))
            .Callback<DisputePhoto, CancellationToken>((p, _) => _saved.Add(p))
            .Returns(Task.CompletedTask);

        _handler = new AddDisputePhotoCommandHandler(_disputeRepo.Object, _storage.Object);
    }

    private Dispute SetupDispute(DisputeStatus status = DisputeStatus.Open)
    {
        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            OpenedByUserId = Opener,
            Reason = "Predmet je stigao oštećen",
            Status = status
        };
        _disputeRepo
            .Setup(r => r.GetByIdAsync(dispute.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dispute);
        return dispute;
    }

    private static AddDisputePhotoCommand Command(
        Guid disputeId, Guid userId, byte[]? bytes = null, string contentType = "image/png")
    {
        var data = bytes ?? PngBytes;
        return new AddDisputePhotoCommand(disputeId, userId, new MemoryStream(data), contentType, data.Length);
    }

    [Fact]
    public async Task Dodavanje_slike_cuva_fajl_u_folder_spora_i_dodeljuje_sledeci_OrderIndex()
    {
        var dispute = SetupDispute();
        dispute.Photos.Add(new DisputePhoto { Id = Guid.NewGuid(), DisputeId = dispute.Id, Url = "/uploads/a.png", OrderIndex = 2 });

        var dto = await _handler.Handle(Command(dispute.Id, Opener), default);

        dto.Url.Should().Be("/uploads/disputes/x/photo.png");
        dto.OrderIndex.Should().Be(3);
        _saved.Should().ContainSingle(p => p.DisputeId == dispute.Id && p.OrderIndex == 3);
        _storage.Verify(s => s.SaveAsync(
            $"disputes/{dispute.Id:N}", ".png", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prva_slika_dobija_OrderIndex_0()
    {
        var dispute = SetupDispute();

        var dto = await _handler.Handle(Command(dispute.Id, Opener), default);

        dto.OrderIndex.Should().Be(0);
    }

    [Fact]
    public async Task Slika_se_moze_dodati_i_dok_je_spor_u_razmatranju()
    {
        var dispute = SetupDispute(DisputeStatus.UnderReview);

        var act = () => _handler.Handle(Command(dispute.Id, Opener), default);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Slika_se_ne_moze_dodati_tudjem_sporu()
    {
        var dispute = SetupDispute();

        var act = () => _handler.Handle(Command(dispute.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Slika_se_ne_moze_dodati_resenom_sporu()
    {
        var dispute = SetupDispute(DisputeStatus.Resolved);

        var act = () => _handler.Handle(Command(dispute.Id, Opener), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Nepostojeci_spor_baca_KeyNotFound()
    {
        var act = () => _handler.Handle(Command(Guid.NewGuid(), Opener), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Spor_moze_imati_najvise_5_slika()
    {
        var dispute = SetupDispute();
        for (var i = 0; i < AddDisputePhotoCommand.MaxPhotosPerDispute; i++)
            dispute.Photos.Add(new DisputePhoto { Id = Guid.NewGuid(), DisputeId = dispute.Id, Url = $"/uploads/{i}.png", OrderIndex = i });

        var act = () => _handler.Handle(Command(dispute.Id, Opener), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sadrzaj_koji_nije_slika_se_odbija()
    {
        var dispute = SetupDispute();
        var notAnImage = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4, 5, 6, 7, 8 }; // "MZ" (exe)

        var act = () => _handler.Handle(Command(dispute.Id, Opener, notAnImage), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ako_upis_u_bazu_ne_uspe_fajl_se_brise_sa_diska()
    {
        var dispute = SetupDispute();
        _disputeRepo
            .Setup(r => r.AddPhotoAsync(It.IsAny<DisputePhoto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db"));

        var act = () => _handler.Handle(Command(dispute.Id, Opener), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _storage.Verify(s => s.DeleteAsync("/uploads/disputes/x/photo.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_odbija_format_koji_nije_slika()
    {
        var result = new AddDisputePhotoCommandValidator().Validate(Command(Guid.NewGuid(), Opener, contentType: "image/gif"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_odbija_prazan_i_prevelik_fajl()
    {
        var validator = new AddDisputePhotoCommandValidator();

        var empty = new AddDisputePhotoCommand(Guid.NewGuid(), Opener, new MemoryStream(), "image/png", 0);
        var tooBig = new AddDisputePhotoCommand(Guid.NewGuid(), Opener, new MemoryStream(PngBytes), "image/png", 6 * 1024 * 1024);

        validator.Validate(empty).IsValid.Should().BeFalse();
        validator.Validate(tooBig).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_prihvata_ispravnu_sliku()
    {
        var result = new AddDisputePhotoCommandValidator().Validate(Command(Guid.NewGuid(), Opener));

        result.IsValid.Should().BeTrue();
    }
}
