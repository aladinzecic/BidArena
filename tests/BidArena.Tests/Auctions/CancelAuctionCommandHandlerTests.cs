using BidArena.Application.Auctions.Commands.CancelAuction;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auctions;

public class CancelAuctionCommandHandlerTests
{
    private static readonly Guid Seller = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CancelAuctionCommandHandler _handler;

    public CancelAuctionCommandHandlerTests()
    {
        _handler = new CancelAuctionCommandHandler(_auctionRepo.Object, _itemRepo.Object, _unitOfWork.Object);
    }

    private Auction Setup(AuctionStatus status, Guid? winner = null)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = Seller, Title = "Sat" };
        var auction = new Auction { Id = Guid.NewGuid(), ItemId = item.Id, Status = status, CurrentWinnerId = winner };

        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        return auction;
    }

    [Theory]
    [InlineData(AuctionStatus.Draft)]
    [InlineData(AuctionStatus.Scheduled)]
    [InlineData(AuctionStatus.Active)]
    public async Task Aukcija_bez_ponuda_moze_da_se_otkaze(AuctionStatus status)
    {
        var auction = Setup(status);

        await _handler.Handle(new CancelAuctionCommand(auction.Id, Seller), default);

        auction.Status.Should().Be(AuctionStatus.Cancelled);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aktivna_aukcija_sa_ponudom_ne_moze_da_se_otkaze()
    {
        var auction = Setup(AuctionStatus.Active, winner: Guid.NewGuid());

        var act = () => _handler.Handle(new CancelAuctionCommand(auction.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auction.Status.Should().Be(AuctionStatus.Active);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(AuctionStatus.Ended)]
    [InlineData(AuctionStatus.Settled)]
    [InlineData(AuctionStatus.Disputed)]
    [InlineData(AuctionStatus.Cancelled)]
    public async Task Aukcija_van_dozvoljenih_statusa_ne_moze_da_se_otkaze(AuctionStatus status)
    {
        var auction = Setup(status);

        var act = () => _handler.Handle(new CancelAuctionCommand(auction.Id, Seller), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auction.Status.Should().Be(status);
    }

    [Fact]
    public async Task Tudju_aukciju_nije_moguce_otkazati()
    {
        var auction = Setup(AuctionStatus.Draft);

        var act = () => _handler.Handle(new CancelAuctionCommand(auction.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        auction.Status.Should().Be(AuctionStatus.Draft);
    }

    [Fact]
    public async Task Nepostojeca_aukcija_baca_KeyNotFound()
    {
        var act = () => _handler.Handle(new CancelAuctionCommand(Guid.NewGuid(), Seller), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
