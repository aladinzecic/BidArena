using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Payments.Commands.PayPayment;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Payments;

public class PayPaymentCommandHandlerTests
{
    private static readonly Guid Winner = Guid.NewGuid();
    private static readonly Guid Seller = Guid.NewGuid();

    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IShipmentRepository> _shipmentRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Notification> _notifications = new();
    private readonly List<Shipment> _shipments = new();
    private readonly PayPaymentCommandHandler _handler;

    public PayPaymentCommandHandlerTests()
    {
        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _shipmentRepo
            .Setup(r => r.AddAsync(It.IsAny<Shipment>(), It.IsAny<CancellationToken>()))
            .Callback<Shipment, CancellationToken>((s, _) => _shipments.Add(s))
            .Returns(Task.CompletedTask);

        _handler = new PayPaymentCommandHandler(
            _paymentRepo.Object, _auctionRepo.Object, _notificationRepo.Object,
            _shipmentRepo.Object, _unitOfWork.Object);
    }

    private Payment Setup(PaymentStatus status = PaymentStatus.Pending, DateTime? dueDate = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            WinnerId = Winner,
            Amount = 150m,
            Commission = 7.50m,
            Status = status,
            DueDate = dueDate ?? DateTime.UtcNow.AddDays(2)
        };

        _paymentRepo
            .Setup(r => r.GetByIdForUpdateAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var auction = new Auction { Id = payment.AuctionId, ItemId = Guid.NewGuid() };
        var item = new Item { Id = auction.ItemId, SellerId = Seller, Title = "Sat" };
        _auctionRepo
            .Setup(r => r.GetWithItemAsync(payment.AuctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuctionWithItem(auction, item, 0, null));

        return payment;
    }

    [Fact]
    public async Task Pobednik_placa_i_prodavac_dobija_obavestenje()
    {
        var payment = Setup();

        await _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.PaidAt.Should().NotBeNull();
        _notifications.Should().ContainSingle(n =>
            n.UserId == Seller && n.Type == NotificationType.PaymentReceived);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Uplata_pravi_posiljku_sa_rokom_za_prodavca()
    {
        var payment = Setup();

        await _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        var shipment = _shipments.Should().ContainSingle().Subject;
        shipment.AuctionId.Should().Be(payment.AuctionId);
        shipment.ShippedAt.Should().BeNull();
        shipment.DueDate.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(PayPaymentCommandHandler.ShipmentDueDays), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Neuspela_uplata_ne_pravi_posiljku()
    {
        var payment = Setup(status: PaymentStatus.Paid);

        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _shipments.Should().BeEmpty();
    }

    [Fact]
    public async Task Tudje_placanje_baca_Forbidden()
    {
        var payment = Setup();

        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(payment.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Vec_placeno_baca_InvalidOperation()
    {
        var payment = Setup(status: PaymentStatus.Paid);

        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Overdue_placanje_se_ne_moze_platiti()
    {
        var payment = Setup(status: PaymentStatus.Overdue, dueDate: DateTime.UtcNow.AddDays(-1));

        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Overdue);
    }

    [Fact]
    public async Task Pending_sa_isteklim_rokom_se_ne_moze_platiti()
    {
        var payment = Setup(dueDate: DateTime.UtcNow.AddMinutes(-1));

        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(payment.Id, Winner), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.PaidAt.Should().BeNull();
    }

    [Fact]
    public async Task Nepostojece_placanje_baca_KeyNotFound()
    {
        Func<Task> act = () => _handler.Handle(new PayPaymentCommand(Guid.NewGuid(), Winner), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}