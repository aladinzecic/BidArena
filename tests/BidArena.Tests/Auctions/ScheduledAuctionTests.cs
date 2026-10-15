using BidArena.Application.Auctions.Commands.ActivateAuction;
using BidArena.Application.Auctions.Commands.StartAuction;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auctions;

public class ScheduledAuctionTests
{
    private static readonly Guid Seller = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private Auction Setup(AuctionStatus status, DateTime start, DateTime end)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = Seller, Title = "Sat" };
        var auction = new Auction
        {
            Id = Guid.NewGuid(), ItemId = item.Id, Status = status, StartTime = start, EndTime = end
        };

        _auctionRepo
            .Setup(r => r.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        return auction;
    }

    [Fact]
    public async Task Aktivacija_sa_pocetkom_u_buducnosti_zakazuje_aukciju()
    {
        var auction = Setup(AuctionStatus.Draft, DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(5));
        var handler = new ActivateAuctionCommandHandler(_auctionRepo.Object, _itemRepo.Object);

        await handler.Handle(new ActivateAuctionCommand(auction.Id, Seller), default);

        auction.Status.Should().Be(AuctionStatus.Scheduled);
    }

    [Fact]
    public async Task Aktivacija_sa_pocetkom_u_proslosti_odmah_pokrece_aukciju()
    {
        var auction = Setup(AuctionStatus.Draft, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(5));
        var handler = new ActivateAuctionCommandHandler(_auctionRepo.Object, _itemRepo.Object);

        await handler.Handle(new ActivateAuctionCommand(auction.Id, Seller), default);

        auction.Status.Should().Be(AuctionStatus.Active);
    }

    [Fact]
    public async Task Zakazana_aukcija_kojoj_je_nastupio_pocetak_postaje_Active()
    {
        var auction = Setup(AuctionStatus.Scheduled, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(5));
        var handler = new StartAuctionCommandHandler(_auctionRepo.Object, _unitOfWork.Object);

        await handler.Handle(new StartAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Active);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Zakazana_aukcija_pre_pocetka_ostaje_Scheduled()
    {
        var auction = Setup(AuctionStatus.Scheduled, DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(5));
        var handler = new StartAuctionCommandHandler(_auctionRepo.Object, _unitOfWork.Object);

        await handler.Handle(new StartAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Scheduled);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Otkazana_aukcija_se_ne_pokrece()
    {
        var auction = Setup(AuctionStatus.Cancelled, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddHours(5));
        var handler = new StartAuctionCommandHandler(_auctionRepo.Object, _unitOfWork.Object);

        await handler.Handle(new StartAuctionCommand(auction.Id), default);

        auction.Status.Should().Be(AuctionStatus.Cancelled);
    }
}
