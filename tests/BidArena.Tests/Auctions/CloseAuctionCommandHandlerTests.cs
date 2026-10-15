using BidArena.Application.Auctions.Commands.CloseAuction;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auctions;

public class CloseAuctionCommandHandlerTests
{
    private static readonly Guid Winner = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Payment> _payments = new();
    private readonly List<Notification> _notifications = new();
    private readonly CloseAuctionCommandHandler _handler;

    public CloseAuctionCommandHandlerTests()
    {
        _paymentRepo
            .Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>((p, _) => _payments.Add(p))
            .Returns(Task.CompletedTask);

        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _handler = new CloseAuctionCommandHandler(
            _auctionRepo.Object, _paymentRepo.Object, _notificationRepo.Object,
            _itemRepo.Object, _unitOfWork.Object);
    }

    private Auction Setup(
        AuctionStatus status = AuctionStatus.Active,
        Guid? winner = null,
        decimal price = 150m,
        decimal? reserve = null,
        DateTime? endTime = null)
    {
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ItemId = Guid.NewGuid(),
            Status = status,
            CurrentWinnerId = winner,
            CurrentPrice = price,
            ReservePrice = reserve,
            StartTime = DateTime.UtcNow.AddHours(-2),
            EndTime = endTime ?? DateTime.UtcNow.AddMinutes(-1)
        };

        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        return auction;
    }

    [Fact]
    public async Task Prodata_aukcija_pravi_placanje_i_obavestenje()
    {
        var auction = Setup(winner: Winner, price: 150m, reserve: 100m);

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.CurrentWinnerId.Should().Be(Winner);

        var payment = _payments.Should().ContainSingle().Subject;
        payment.WinnerId.Should().Be(Winner);
        payment.Amount.Should().Be(150m);
        payment.Commission.Should().Be(7.50m);
        payment.Status.Should().Be(PaymentStatus.Pending);

        _notifications.Should().ContainSingle(n => n.UserId == Winner && n.Type == NotificationType.AuctionWon);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Neprodata_aukcija_ne_pravi_obavestenje_ni_placanje()
    {
        var auction = Setup(winner: null);

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        _notifications.Should().BeEmpty();
        _payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Produzena_aukcija_se_ne_zatvara()
    {
        var auction = Setup(winner: Winner, endTime: DateTime.UtcNow.AddMinutes(8));

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Active);
        _notifications.Should().BeEmpty();
        _payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Rezervna_cena_nije_dostignuta_zavrsava_bez_pobednika()
    {
        var auction = Setup(winner: Winner, price: 150m, reserve: 200m);

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.CurrentWinnerId.Should().BeNull();
        _payments.Should().BeEmpty();
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Aukcija_bez_ponuda_zavrsava_bez_placanja()
    {
        var auction = Setup(winner: null);

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Ended);
        _payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Aukcija_produzena_anti_snipe_ponudom_se_ne_zatvara()
    {
        var auction = Setup(winner: Winner, endTime: DateTime.UtcNow.AddMinutes(8));

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Active);
        _payments.Should().BeEmpty();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Vec_zatvorena_aukcija_se_ne_obradjuje_ponovo()
    {
        var auction = Setup(status: AuctionStatus.Ended, winner: Winner);

        await _handler.Handle(new CloseAuctionCommand(auction.Id), default);

        _payments.Should().BeEmpty();
        _notifications.Should().BeEmpty();
    }
}