using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Commands.ResolveDispute;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Disputes;

public class DisputeOutcomeTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();
    private const string Text = "Predmet nije kako je opisano";

    private readonly Mock<IDisputeRepository> _disputeRepo = new();
    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IPaymentRepository> _paymentRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ResolveDisputeCommandHandler _handler;

    public DisputeOutcomeTests()
    {
        _handler = new ResolveDisputeCommandHandler(
            _disputeRepo.Object, _auctionRepo.Object, _itemRepo.Object, _paymentRepo.Object,
            _userRepo.Object, _notificationRepo.Object, _unitOfWork.Object);
    }

    private (Auction, Dispute, Payment, User) Setup()
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction { Id = Guid.NewGuid(), ItemId = item.Id, Status = AuctionStatus.Disputed, CurrentWinnerId = BuyerId };
        var dispute = new Dispute { Id = Guid.NewGuid(), AuctionId = auction.Id, OpenedByUserId = BuyerId, Status = DisputeStatus.UnderReview };
        var payment = new Payment { Id = Guid.NewGuid(), AuctionId = auction.Id, WinnerId = BuyerId, Status = PaymentStatus.Paid };
        var seller = new User { Id = SellerId };

        _disputeRepo.Setup(r => r.GetByIdForUpdateAsync(dispute.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dispute);
        _auctionRepo.Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(auction);
        _itemRepo.Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        _paymentRepo.Setup(r => r.GetByAuctionIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>())).ReturnsAsync(payment);
        _userRepo.Setup(r => r.GetByIdAsync(SellerId, It.IsAny<CancellationToken>())).ReturnsAsync(seller);

        return (auction, dispute, payment, seller);
    }

    [Fact]
    public async Task Povracaj_kupcu_vraca_novac_otkazuje_aukciju_i_kaznjava_prodavca()
    {
        var (auction, dispute, payment, _) = Setup();

        await _handler.Handle(
            new ResolveDisputeCommand(dispute.Id, Guid.NewGuid(), Text, DisputeOutcome.RefundBuyer), default);

        payment.Status.Should().Be(PaymentStatus.Refunded);
        auction.Status.Should().Be(AuctionStatus.Cancelled);
        dispute.Status.Should().Be(DisputeStatus.Resolved);
        dispute.Resolution.Should().StartWith("[Povraćaj novca kupcu]");
        _userRepo.Verify(r => r.IncrementCancelledAuctionsAsync(SellerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task U_korist_prodavca_aukcija_je_Settled_a_uplata_ostaje()
    {
        var (auction, dispute, payment, seller) = Setup();

        await _handler.Handle(
            new ResolveDisputeCommand(dispute.Id, Guid.NewGuid(), Text, DisputeOutcome.FavorSeller), default);

        payment.Status.Should().Be(PaymentStatus.Paid);
        auction.Status.Should().Be(AuctionStatus.Settled);
        seller.SuccessfulAuctionsCount.Should().Be(1);
    }
}
