using BidArena.Application.Items.Commands.AddItemPhoto;
using BidArena.Application.Items.Commands.UpdateItem;
using FluentAssertions;

namespace BidArena.Tests.Validators;

public class ItemPhotoAndUpdateValidatorTests
{
    private readonly AddItemPhotoCommandValidator _photoValidator = new();
    private readonly UpdateItemCommandValidator _updateValidator = new();

    private static AddItemPhotoCommand Photo(string contentType, long length) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new MemoryStream(), contentType, length);

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("IMAGE/PNG")]
    public void Dozvoljeni_formati_prolaze(string contentType) =>
        _photoValidator.Validate(Photo(contentType, 1024)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public void Nedozvoljeni_formati_ne_prolaze(string contentType) =>
        _photoValidator.Validate(Photo(contentType, 1024)).IsValid.Should().BeFalse();

    [Fact]
    public void Prazan_fajl_ne_prolazi() =>
        _photoValidator.Validate(Photo("image/png", 0)).IsValid.Should().BeFalse();

    [Fact]
    public void Fajl_veci_od_5MB_ne_prolazi() =>
        _photoValidator.Validate(Photo("image/png", AddItemPhotoCommand.MaxSizeBytes + 1)).IsValid.Should().BeFalse();

    [Fact]
    public void Fajl_od_tacno_5MB_prolazi() =>
        _photoValidator.Validate(Photo("image/png", AddItemPhotoCommand.MaxSizeBytes)).IsValid.Should().BeTrue();

    [Fact]
    public void Ispravna_izmena_predmeta_prolazi() =>
        _updateValidator.Validate(new UpdateItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Naslov", "Opis", "Satovi", "Novo", "Beograd"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Nepoznata_kategorija_u_izmeni_ne_prolazi() =>
        _updateValidator.Validate(new UpdateItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Naslov", "Opis", "Nesto", "Novo", "Beograd"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void Nepoznato_stanje_u_izmeni_ne_prolazi() =>
        _updateValidator.Validate(new UpdateItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Naslov", "Opis", "Satovi", "99", "Beograd"))
            .IsValid.Should().BeFalse();
}
