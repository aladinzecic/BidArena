using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Shipments.Commands.ConfirmDelivery;
using BidArena.Application.Shipments.Commands.MarkShipped;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Shipments;

public class ShipmentFlowTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IShipmentRepository> _shipmentRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Notification> _notifications = new();

    private readonly MarkShippedCommandHandler _shipHandler;
    private readonly ConfirmDeliveryCommandHandler _confirmHandler;

    public ShipmentFlowTests()
    {
        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _shipHandler = new MarkShippedCommandHandler(
            _auctionRepo.Object, _itemRepo.Object, _shipmentRepo.Object,
            _notificationRepo.Object, _unitOfWork.Object);

        _confirmHandler = new ConfirmDeliveryCommandHandler(
            _auctionRepo.Object, _itemRepo.Object, _shipmentRepo.Object, _userRepo.Object,
            _notificationRepo.Object, _unitOfWork.Object);
    }

    private (Auction Auction, Shipment Shipment, User Seller) Setup(
        AuctionStatus status = AuctionStatus.Ended,
        DateTime? shippedAt = null,
        DateTime? deliveredAt = null)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Status = status,
            CurrentWinnerId = BuyerId
        };
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            DueDate = DateTime.UtcNow.AddDays(5),
            ShippedAt = shippedAt,
            DeliveredAt = deliveredAt
        };
        var seller = new User { Id = SellerId };

        _auctionRepo.Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _shipmentRepo.Setup(r => r.GetByAuctionIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _userRepo.Setup(r => r.GetByIdAsync(SellerId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        return (auction, shipment, seller);
    }

    // ---------- MarkShipped ----------

    [Fact]
    public async Task Prodavac_salje_pa_kupac_dobija_obavestenje()
    {
        var (auction, shipment, _) = Setup();

        await _shipHandler.Handle(new MarkShippedCommand(auction.Id, SellerId, "  RS123  "), default);

        shipment.TrackingNumber.Should().Be("RS123");
        shipment.ShippedAt.Should().NotBeNull();
        _notifications.Should().ContainSingle(n => n.UserId == BuyerId && n.Type == NotificationType.ItemShipped);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Slanje_tudjeg_predmeta_baca_Forbidden()
    {
        var (auction, shipment, _) = Setup();

        Func<Task> act = () => _shipHandler.Handle(new MarkShippedCommand(auction.Id, Guid.NewGuid(), "RS1"), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        shipment.ShippedAt.Should().BeNull();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Slanje_bez_uplate_baca_KeyNotFound()
    {
        var (auction, _, _) = Setup();
        _shipmentRepo
            .Setup(r => r.GetByAuctionIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shipment?)null);

        Func<Task> act = () => _shipHandler.Handle(new MarkShippedCommand(auction.Id, SellerId, "RS1"), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Dvostruko_slanje_baca_InvalidOperation()
    {
        var (auction, _, _) = Setup(shippedAt: DateTime.UtcNow.AddHours(-1));

        Func<Task> act = () => _shipHandler.Handle(new MarkShippedCommand(auction.Id, SellerId, "RS2"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Slanje_tokom_spora_baca_InvalidOperation()
    {
        var (auction, shipment, _) = Setup(status: AuctionStatus.Disputed);

        Func<Task> act = () => _shipHandler.Handle(new MarkShippedCommand(auction.Id, SellerId, "RS1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        shipment.ShippedAt.Should().BeNull();
    }

    // ---------- ConfirmDelivery ----------

    [Fact]
    public async Task Potvrda_isporuke_zatvara_aukciju_i_broji_uspesnu_prodaju()
    {
        var (auction, shipment, seller) = Setup(shippedAt: DateTime.UtcNow.AddDays(-1));

        await _confirmHandler.Handle(new ConfirmDeliveryCommand(auction.Id, BuyerId), default);

        shipment.DeliveredAt.Should().NotBeNull();
        auction.Status.Should().Be(AuctionStatus.Settled);
        seller.SuccessfulAuctionsCount.Should().Be(1);
        _notifications.Should().ContainSingle(n => n.UserId == SellerId && n.Type == NotificationType.ItemDelivered);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Potvrda_od_nepobednika_baca_Forbidden()
    {
        var (auction, _, _) = Setup(shippedAt: DateTime.UtcNow.AddDays(-1));

        Func<Task> act = () => _confirmHandler.Handle(new ConfirmDeliveryCommand(auction.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        auction.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task Potvrda_pre_slanja_baca_InvalidOperation()
    {
        var (auction, _, _) = Setup();

        Func<Task> act = () => _confirmHandler.Handle(new ConfirmDeliveryCommand(auction.Id, BuyerId), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auction.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task Ponovna_potvrda_baca_InvalidOperation()
    {
        var (auction, _, seller) = Setup(
            status: AuctionStatus.Ended,
            shippedAt: DateTime.UtcNow.AddDays(-2),
            deliveredAt: DateTime.UtcNow.AddDays(-1));

        Func<Task> act = () => _confirmHandler.Handle(new ConfirmDeliveryCommand(auction.Id, BuyerId), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        seller.SuccessfulAuctionsCount.Should().Be(0);
    }
}
