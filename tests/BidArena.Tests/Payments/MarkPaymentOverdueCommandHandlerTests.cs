using BidArena.Application.Common.Interfaces;
using BidArena.Application.Payments.Commands.MarkPaymentOverdue;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Payments;

public class MarkPaymentOverdueCommandHandlerTests
{
    private static readonly Guid Winner = Guid.NewGuid();

    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Notification> _notifications = new();
    private readonly MarkPaymentOverdueCommandHandler _handler;

    public MarkPaymentOverdueCommandHandlerTests()
    {
        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _handler = new MarkPaymentOverdueCommandHandler(
            _paymentRepo.Object, _notificationRepo.Object, _auctionRepo.Object,
            _itemRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    private Payment Setup(PaymentStatus status, DateTime dueDate)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AuctionId = Guid.NewGuid(),
            WinnerId = Winner,
            Amount = 150m,
            Commission = 7.50m,
            Status = status,
            DueDate = dueDate
        };

        _paymentRepo
            .Setup(r => r.GetByIdForUpdateAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        return payment;
    }

    [Fact]
    public async Task Pending_sa_isteklim_rokom_postaje_Overdue_i_pobednik_dobija_obavestenje()
    {
        var payment = Setup(PaymentStatus.Pending, DateTime.UtcNow.AddMinutes(-5));

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        payment.Status.Should().Be(PaymentStatus.Overdue);
        _notifications.Should().ContainSingle(n => n.UserId == Winner && n.Type == NotificationType.PaymentDue);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Istek_placanja_otkazuje_aukciju_penalizuje_kupca_i_obavestava_prodavca()
    {
        var seller = Guid.NewGuid();
        var payment = Setup(PaymentStatus.Pending, DateTime.UtcNow.AddMinutes(-5));
        var auction = new Auction { Id = payment.AuctionId, ItemId = Guid.NewGuid(), Status = AuctionStatus.Ended };
        var item = new Item { Id = auction.ItemId, SellerId = seller, Title = "Ručni sat" };

        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        auction.Status.Should().Be(AuctionStatus.Cancelled);
        _userRepo.Verify(r => r.IncrementCancelledAuctionsAsync(Winner, It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Should().Contain(n => n.UserId == seller && n.Type == NotificationType.AuctionCancelled);
        _notifications.Should().Contain(n => n.UserId == Winner && n.Type == NotificationType.PaymentDue);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aukcija_u_sporu_se_ne_otkazuje_zbog_isteka_placanja()
    {
        var payment = Setup(PaymentStatus.Pending, DateTime.UtcNow.AddMinutes(-5));
        var auction = new Auction { Id = payment.AuctionId, ItemId = Guid.NewGuid(), Status = AuctionStatus.Disputed };

        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        payment.Status.Should().Be(PaymentStatus.Overdue);
        auction.Status.Should().Be(AuctionStatus.Disputed);
        _userRepo.Verify(r => r.IncrementCancelledAuctionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rok_jos_nije_istekao_nista_se_ne_menja()
    {
        var payment = Setup(PaymentStatus.Pending, DateTime.UtcNow.AddDays(1));

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        payment.Status.Should().Be(PaymentStatus.Pending);
        _notifications.Should().BeEmpty();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Placeno_se_ne_prebacuje_u_Overdue()
    {
        var payment = Setup(PaymentStatus.Paid, DateTime.UtcNow.AddMinutes(-5));

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        payment.Status.Should().Be(PaymentStatus.Paid);
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Vec_Overdue_ne_salje_drugo_obavestenje()
    {
        var payment = Setup(PaymentStatus.Overdue, DateTime.UtcNow.AddDays(-1));

        await _handler.Handle(new MarkPaymentOverdueCommand(payment.Id), default);

        _notifications.Should().BeEmpty();
    }
}