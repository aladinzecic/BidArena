using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Commands.OpenDispute;
using BidArena.Application.Disputes.Commands.ResolveDispute;
using BidArena.Application.Disputes.Commands.TakeDispute;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Disputes;

public class DisputeFlowTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    private const string Reason = "Predmet nije kako je opisano";
    private const string Resolution = "Odluka je doneta u korist kupca";

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IDisputeRepository> _disputeRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Dispute> _addedDisputes = new();
    private readonly List<Notification> _notifications = new();

    private readonly OpenDisputeCommandHandler _openHandler;
    private readonly TakeDisputeCommandHandler _takeHandler;
    private readonly ResolveDisputeCommandHandler _resolveHandler;

    public DisputeFlowTests()
    {
        _disputeRepo
            .Setup(r => r.AddAsync(It.IsAny<Dispute>(), It.IsAny<CancellationToken>()))
            .Callback<Dispute, CancellationToken>((d, _) => _addedDisputes.Add(d))
            .Returns(Task.CompletedTask);

        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _openHandler = new OpenDisputeCommandHandler(
            _auctionRepo.Object, _itemRepo.Object, _paymentRepo.Object,
            _disputeRepo.Object, _notificationRepo.Object, _unitOfWork.Object);

        _takeHandler = new TakeDisputeCommandHandler(_disputeRepo.Object, _unitOfWork.Object);

        _resolveHandler = new ResolveDisputeCommandHandler(
            _disputeRepo.Object, _auctionRepo.Object, _itemRepo.Object,
            _paymentRepo.Object, _userRepo.Object, _notificationRepo.Object, _unitOfWork.Object);
    }

    private Auction SetupAuction(
        AuctionStatus status = AuctionStatus.Ended,
        PaymentStatus? paymentStatus = PaymentStatus.Paid)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Status = status,
            CurrentWinnerId = BuyerId
        };

        _auctionRepo.Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var payment = paymentStatus is null
            ? null
            : new Payment { Id = Guid.NewGuid(), AuctionId = auction.Id, WinnerId = BuyerId, Status = paymentStatus.Value };
        _paymentRepo.Setup(r => r.GetByAuctionIdAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);

        return auction;
    }

    private Dispute SetupDispute(Auction auction, DisputeStatus status)
    {
        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            OpenedByUserId = BuyerId,
            Reason = Reason,
            Status = status
        };

        _disputeRepo.Setup(r => r.GetByIdForUpdateAsync(dispute.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);
        return dispute;
    }

    // ---------- OpenDispute ----------

    [Fact]
    public async Task Kupac_otvara_spor_aukcija_postaje_Disputed_a_prodavac_dobija_obavestenje()
    {
        var auction = SetupAuction();

        var id = await _openHandler.Handle(new OpenDisputeCommand(auction.Id, BuyerId, "  " + Reason + "  "), default);

        var dispute = _addedDisputes.Should().ContainSingle().Subject;
        dispute.Id.Should().Be(id);
        dispute.Status.Should().Be(DisputeStatus.Open);
        dispute.Reason.Should().Be(Reason);
        dispute.OpenedByUserId.Should().Be(BuyerId);
        auction.Status.Should().Be(AuctionStatus.Disputed);
        _notifications.Should().ContainSingle(n => n.UserId == SellerId && n.Type == NotificationType.DisputeOpened);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prodavac_otvara_spor_kupac_dobija_obavestenje()
    {
        var auction = SetupAuction();

        await _openHandler.Handle(new OpenDisputeCommand(auction.Id, SellerId, Reason), default);

        _notifications.Should().ContainSingle(n => n.UserId == BuyerId && n.Type == NotificationType.DisputeOpened);
    }

    [Fact]
    public async Task Spor_od_treceg_lica_baca_Forbidden()
    {
        var auction = SetupAuction();

        Func<Task> act = () => _openHandler.Handle(new OpenDisputeCommand(auction.Id, Guid.NewGuid(), Reason), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _addedDisputes.Should().BeEmpty();
        auction.Status.Should().Be(AuctionStatus.Ended);
    }

    [Fact]
    public async Task Spor_pre_placanja_baca_InvalidOperation()
    {
        var auction = SetupAuction(paymentStatus: PaymentStatus.Pending);

        Func<Task> act = () => _openHandler.Handle(new OpenDisputeCommand(auction.Id, BuyerId, Reason), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _addedDisputes.Should().BeEmpty();
    }

    [Fact]
    public async Task Spor_bez_placanja_uopste_baca_InvalidOperation()
    {
        var auction = SetupAuction(paymentStatus: null);

        Func<Task> act = () => _openHandler.Handle(new OpenDisputeCommand(auction.Id, BuyerId, Reason), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(AuctionStatus.Disputed)]
    [InlineData(AuctionStatus.Settled)]
    [InlineData(AuctionStatus.Active)]
    public async Task Spor_je_dozvoljen_samo_za_aukciju_u_statusu_Ended(AuctionStatus status)
    {
        var auction = SetupAuction(status: status);

        Func<Task> act = () => _openHandler.Handle(new OpenDisputeCommand(auction.Id, BuyerId, Reason), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _addedDisputes.Should().BeEmpty();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- TakeDispute ----------

    [Fact]
    public async Task Admin_preuzima_otvoren_spor()
    {
        var dispute = SetupDispute(SetupAuction(AuctionStatus.Disputed), DisputeStatus.Open);

        await _takeHandler.Handle(new TakeDisputeCommand(dispute.Id), default);

        dispute.Status.Should().Be(DisputeStatus.UnderReview);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Preuzimanje_spora_koji_nije_Open_baca_InvalidOperation()
    {
        var dispute = SetupDispute(SetupAuction(AuctionStatus.Disputed), DisputeStatus.UnderReview);

        Func<Task> act = () => _takeHandler.Handle(new TakeDisputeCommand(dispute.Id), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Preuzimanje_nepostojeceg_spora_baca_KeyNotFound()
    {
        Func<Task> act = () => _takeHandler.Handle(new TakeDisputeCommand(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ---------- ResolveDispute ----------

    [Fact]
    public async Task Resavanje_zatvara_spor_i_aukciju_a_obe_strane_dobijaju_obavestenje()
    {
        var auction = SetupAuction(AuctionStatus.Disputed);
        var dispute = SetupDispute(auction, DisputeStatus.UnderReview);

        await _resolveHandler.Handle(new ResolveDisputeCommand(dispute.Id, AdminId, "  " + Resolution + "  "), default);

        dispute.Status.Should().Be(DisputeStatus.Resolved);
        dispute.Resolution.Should().Be("[U korist prodavca] " + Resolution);
        dispute.ResolvedByAdminId.Should().Be(AdminId);
        auction.Status.Should().Be(AuctionStatus.Settled);
        _notifications.Should().HaveCount(2);
        _notifications.Should().Contain(n => n.UserId == SellerId && n.Type == NotificationType.DisputeResolved);
        _notifications.Should().Contain(n => n.UserId == BuyerId && n.Type == NotificationType.DisputeResolved);
    }

    [Theory]
    [InlineData(DisputeStatus.Open)]
    [InlineData(DisputeStatus.Resolved)]
    public async Task Resavanje_je_dozvoljeno_samo_iz_UnderReview(DisputeStatus status)
    {
        var auction = SetupAuction(AuctionStatus.Disputed);
        var dispute = SetupDispute(auction, status);

        Func<Task> act = () => _resolveHandler.Handle(new ResolveDisputeCommand(dispute.Id, AdminId, Resolution), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auction.Status.Should().Be(AuctionStatus.Disputed);
        _notifications.Should().BeEmpty();
    }
}
