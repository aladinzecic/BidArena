using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Items.Commands.AddItemPhoto;
using BidArena.Application.Items.Commands.DeleteItem;
using BidArena.Application.Items.Commands.RemoveItemPhoto;
using BidArena.Application.Items.Commands.UpdateItem;
using BidArena.Application.Items.Queries.GetItemById;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Items;

public class ItemManagementTests
{
    private static readonly Guid Seller = Guid.NewGuid();
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };

    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IFileStorage> _storage = new();

    private Item SetupItem(params AuctionStatus[] auctionStatuses)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = Seller, Title = "Sat" };

        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _auctionRepo
            .Setup(r => r.GetStatusesByItemIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auctionStatuses.ToList());
        _storage
            .Setup(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/x/photo.png");

        return item;
    }

    private static UpdateItemCommand Update(Guid itemId, Guid seller, string condition = "Koriscen") =>
        new(itemId, seller, "Novi naslov", "Novi opis", "Satovi", condition, "Beograd");

    // ---------- UpdateItem ----------

    [Fact]
    public async Task Izmena_menja_polja_predmeta()
    {
        var item = SetupItem();
        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _auctionRepo.Object);

        await handler.Handle(Update(item.Id, Seller), default);

        item.Title.Should().Be("Novi naslov");
        item.Condition.Should().Be(ItemCondition.Koriscen);
        item.Location.Should().Be("Beograd");
        _itemRepo.Verify(r => r.UpdateAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Izmena_je_dozvoljena_ako_su_aukcije_samo_Draft_ili_Cancelled()
    {
        var item = SetupItem(AuctionStatus.Draft, AuctionStatus.Cancelled);
        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _auctionRepo.Object);

        await handler.Handle(Update(item.Id, Seller), default);

        item.Title.Should().Be("Novi naslov");
    }

    [Theory]
    [InlineData(AuctionStatus.Scheduled)]
    [InlineData(AuctionStatus.Active)]
    [InlineData(AuctionStatus.Ended)]
    [InlineData(AuctionStatus.Settled)]
    [InlineData(AuctionStatus.Disputed)]
    public async Task Izmena_nije_dozvoljena_dok_aukcija_nije_Draft_ili_Cancelled(AuctionStatus status)
    {
        var item = SetupItem(status);
        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _auctionRepo.Object);

        var act = () => handler.Handle(Update(item.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        item.Title.Should().Be("Sat");
    }

    [Fact]
    public async Task Tudj_predmet_nije_moguce_menjati()
    {
        var item = SetupItem();
        var handler = new UpdateItemCommandHandler(_itemRepo.Object, _auctionRepo.Object);

        var act = () => handler.Handle(Update(item.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ---------- DeleteItem ----------

    [Fact]
    public async Task Brisanje_predmeta_brise_red_i_fajlove_slika()
    {
        var item = SetupItem();
        item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = "/uploads/a.png" });
        item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = "/uploads/b.png" });
        var handler = new DeleteItemCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        await handler.Handle(new DeleteItemCommand(item.Id, Seller), default);

        _itemRepo.Verify(r => r.DeleteAsync(item, It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync("/uploads/a.png", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync("/uploads/b.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Predmet_koji_ima_aukciju_ne_moze_da_se_obrise()
    {
        var item = SetupItem(AuctionStatus.Cancelled);
        var handler = new DeleteItemCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(new DeleteItemCommand(item.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _itemRepo.Verify(r => r.DeleteAsync(It.IsAny<Item>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Tudj_predmet_nije_moguce_obrisati()
    {
        var item = SetupItem();
        var handler = new DeleteItemCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(new DeleteItemCommand(item.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ---------- AddItemPhoto ----------

    private AddItemPhotoCommand AddPhoto(Guid itemId, Guid seller, byte[]? bytes = null, string contentType = "image/png")
    {
        var data = bytes ?? PngBytes;
        return new AddItemPhotoCommand(itemId, seller, new MemoryStream(data), contentType, data.Length);
    }

    [Fact]
    public async Task Dodavanje_slike_cuva_fajl_i_dodeljuje_sledeci_OrderIndex()
    {
        var item = SetupItem();
        item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = "/uploads/a.png", OrderIndex = 4 });
        var saved = new List<ItemPhoto>();
        _itemRepo
            .Setup(r => r.AddPhotoAsync(It.IsAny<ItemPhoto>(), It.IsAny<CancellationToken>()))
            .Callback<ItemPhoto, CancellationToken>((p, _) => saved.Add(p))
            .Returns(Task.CompletedTask);
        var handler = new AddItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var dto = await handler.Handle(AddPhoto(item.Id, Seller), default);

        dto.Url.Should().Be("/uploads/x/photo.png");
        dto.OrderIndex.Should().Be(5);
        saved.Should().ContainSingle(p => p.ItemId == item.Id && p.OrderIndex == 5);
        _storage.Verify(s => s.SaveAsync(item.Id.ToString("N"), ".png", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sadrzaj_koji_nije_slika_se_odbija()
    {
        var item = SetupItem();
        var handler = new AddItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);
        var notAnImage = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4, 5, 6, 7, 8 }; // "MZ" (exe)

        var act = () => handler.Handle(AddPhoto(item.Id, Seller, notAnImage), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _storage.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Predmet_moze_imati_najvise_10_slika()
    {
        var item = SetupItem();
        for (var i = 0; i < 10; i++)
            item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = $"/uploads/{i}.png", OrderIndex = i });
        var handler = new AddItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(AddPhoto(item.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Ako_upis_u_bazu_ne_uspe_fajl_se_brise_sa_diska()
    {
        var item = SetupItem();
        _itemRepo
            .Setup(r => r.AddPhotoAsync(It.IsAny<ItemPhoto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db"));
        var handler = new AddItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(AddPhoto(item.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _storage.Verify(s => s.DeleteAsync("/uploads/x/photo.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Slika_se_ne_moze_dodati_tudjem_predmetu()
    {
        var item = SetupItem();
        var handler = new AddItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(AddPhoto(item.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ---------- RemoveItemPhoto ----------

    [Fact]
    public async Task Brisanje_slike_brise_red_i_fajl()
    {
        var item = SetupItem();
        var photo = new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = "/uploads/a.png" };
        item.Photos.Add(photo);
        var handler = new RemoveItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        await handler.Handle(new RemoveItemPhotoCommand(item.Id, photo.Id, Seller), default);

        _itemRepo.Verify(r => r.RemovePhotoAsync(photo, It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.DeleteAsync("/uploads/a.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nepostojeca_slika_baca_KeyNotFound()
    {
        var item = SetupItem();
        var handler = new RemoveItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(new RemoveItemPhotoCommand(item.Id, Guid.NewGuid(), Seller), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Slika_se_ne_moze_obrisati_dok_predmet_ima_aktivnu_aukciju()
    {
        var item = SetupItem(AuctionStatus.Active);
        var photo = new ItemPhoto { Id = Guid.NewGuid(), ItemId = item.Id, Url = "/uploads/a.png" };
        item.Photos.Add(photo);
        var handler = new RemoveItemPhotoCommandHandler(_itemRepo.Object, _auctionRepo.Object, _storage.Object);

        var act = () => handler.Handle(new RemoveItemPhotoCommand(item.Id, photo.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---------- GetItemById ----------

    [Fact]
    public async Task Pregled_predmeta_vraca_slike_poredjane_po_OrderIndex()
    {
        var item = SetupItem();
        item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), Url = "/uploads/2.png", OrderIndex = 2 });
        item.Photos.Add(new ItemPhoto { Id = Guid.NewGuid(), Url = "/uploads/1.png", OrderIndex = 1 });
        var handler = new GetItemByIdQueryHandler(_itemRepo.Object);

        var dto = await handler.Handle(new GetItemByIdQuery(item.Id, Seller), default);

        dto.Photos.Select(p => p.Url).Should().Equal("/uploads/1.png", "/uploads/2.png");
    }

    [Fact]
    public async Task Tudj_predmet_nije_moguce_pregledati()
    {
        var item = SetupItem();
        var handler = new GetItemByIdQueryHandler(_itemRepo.Object);

        var act = () => handler.Handle(new GetItemByIdQuery(item.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
