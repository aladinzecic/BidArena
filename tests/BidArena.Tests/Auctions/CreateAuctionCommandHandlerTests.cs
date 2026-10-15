using BidArena.Application.Auctions.Commands.CreateAuction;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auctions;

public class CreateAuctionCommandHandlerTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly List<Auction> _savedAuctions = new();
    private readonly CreateAuctionCommandHandler _handler;

    public CreateAuctionCommandHandlerTests()
    {
        _auctionRepo
            .Setup(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()))
            .Callback<Auction, CancellationToken>((a, _) => _savedAuctions.Add(a))
            .ReturnsAsync((Auction a, CancellationToken _) => a);

        _userRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new User { Id = id, IsVerified = true });

        _handler = new CreateAuctionCommandHandler(_auctionRepo.Object, _itemRepo.Object, _userRepo.Object);
    }

    private Item ItemOwnedBy(Guid sellerId)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = sellerId, Title = "Ručni sat" };

        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        return item;
    }

    private static CreateAuctionCommand Command(
        Guid sellerId, Guid itemId, string type = "English", DateTime? start = null, DateTime? end = null)
    {
        var startTime = start ?? DateTime.UtcNow.AddHours(1);
        return new CreateAuctionCommand(
            sellerId, itemId, type, 100m, 150m, 5m, startTime, end ?? startTime.AddDays(3));
    }

    [Fact]
    public async Task Tudj_predmet_baca_ForbiddenAccessException_i_ne_pravi_aukciju()
    {
        var item = ItemOwnedBy(Owner);

        Func<Task> act = () => _handler.Handle(Command(Stranger, item.Id), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _auctionRepo.Verify(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Nepostojeci_predmet_baca_KeyNotFoundException()
    {
        // GetByIdAsync nije podešen: Moq vraća null.
        Func<Task> act = () => _handler.Handle(Command(Owner, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _auctionRepo.Verify(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Vlasnik_pravi_aukciju_u_Draft_statusu()
    {
        var item = ItemOwnedBy(Owner);
        var command = Command(Owner, item.Id, "English");

        var id = await _handler.Handle(command, default);

        var auction = _savedAuctions.Should().ContainSingle().Subject;
        auction.Id.Should().Be(id);
        auction.ItemId.Should().Be(item.Id);
        auction.Type.Should().Be(AuctionType.English);
        auction.Status.Should().Be(AuctionStatus.Draft);
        auction.StartingPrice.Should().Be(100m);
        auction.CurrentPrice.Should().Be(100m);
        auction.ReservePrice.Should().Be(150m);
        auction.BidIncrement.Should().Be(5m);
        auction.StartTime.Should().Be(command.StartTime);
        auction.EndTime.Should().Be(command.EndTime);
    }

    [Fact]
    public async Task Nepoznat_tip_aukcije_baca_ArgumentException()
    {
        var item = ItemOwnedBy(Owner);

        Func<Task> act = () => _handler.Handle(Command(Owner, item.Id, "Sealed"), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _auctionRepo.Verify(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Kraj_pre_pocetka_baca_ArgumentException()
    {
        var item = ItemOwnedBy(Owner);
        var start = DateTime.UtcNow.AddHours(2);

        Func<Task> act = () => _handler.Handle(
            Command(Owner, item.Id, start: start, end: start.AddMinutes(-1)), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _auctionRepo.Verify(r => r.AddAsync(It.IsAny<Auction>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
