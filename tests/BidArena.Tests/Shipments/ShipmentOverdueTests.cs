using BidArena.Application.Common.Interfaces;
using BidArena.Application.Shipments.Commands.MarkShipmentOverdue;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Shipments;

public class ShipmentOverdueTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IShipmentRepository> _shipmentRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Notification> _notifications = new();
    private readonly MarkShipmentOverdueCommandHandler _handler;

    public ShipmentOverdueTests()
    {
        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _handler = new MarkShipmentOverdueCommandHandler(
            _auctionRepo.Object, _itemRepo.Object, _shipmentRepo.Object, _paymentRepo.Object,
            _userRepo.Object, _notificationRepo.Object, _unitOfWork.Object);
    }

    private (Auction auction, Shipment shipment, Payment payment) Setup(
        AuctionStatus status = AuctionStatus.Ended,
        DateTime? shippedAt = null,
        TimeSpan? dueIn = null)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction { Id = Guid.NewGuid(), ItemId = item.Id, Status = status, CurrentWinnerId = BuyerId };
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            ShippedAt = shippedAt,
            DueDate = DateTime.UtcNow.Add(dueIn ?? TimeSpan.FromHours(-1))
        };
        var payment = new Payment { Id = Guid.NewGuid(), AuctionId = auction.Id, WinnerId = BuyerId, Status = PaymentStatus.Paid };

        _auctionRepo.Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _shipmentRepo.Setup(r => r.GetByAuctionIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(shipment);
        _paymentRepo.Setup(r => r.GetByAuctionIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        return (auction, shipment, payment);
    }

    [Fact]
    public async Task Neposlata_posiljka_otkazuje_aukciju_vraca_novac_i_kaznjava_prodavca()
    {
        var (auction, _, payment) = Setup();

        await _handler.Handle(new MarkShipmentOverdueCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Cancelled);
        payment.Status.Should().Be(PaymentStatus.Refunded);
        _userRepo.Verify(r => r.IncrementCancelledAuctionsAsync(SellerId, It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Should().HaveCount(2);
        _notifications.Should().Contain(n => n.UserId == SellerId);
        _notifications.Should().Contain(n => n.UserId == BuyerId);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Poslata_posiljka_se_ne_otkazuje()
    {
        var (auction, _, payment) = Setup(shippedAt: DateTime.UtcNow.AddHours(-2));

        await _handler.Handle(new MarkShipmentOverdueCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Ended);
        payment.Status.Should().Be(PaymentStatus.Paid);
        _notifications.Should().BeEmpty();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rok_koji_nije_istekao_ne_otkazuje_aukciju()
    {
        var (auction, _, _) = Setup(dueIn: TimeSpan.FromDays(2));

        await _handler.Handle(new MarkShipmentOverdueCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Ended);
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Aukcija_u_sporu_se_ne_otkazuje()
    {
        var (auction, _, _) = Setup(status: AuctionStatus.Disputed);

        await _handler.Handle(new MarkShipmentOverdueCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Disputed);
        _notifications.Should().BeEmpty();
    }
}
