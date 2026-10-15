using BidArena.Application.Bids.Commands.PlaceBid;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Auctions;

public class DutchAuctionTests
{
    private static readonly Guid Buyer = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IBidRepository> _bidRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly PlaceBidCommandHandler _handler;

    public DutchAuctionTests()
    {
        _userRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new User { Id = id, IsVerified = true, HasPaymentMethod = true });

        _handler = new PlaceBidCommandHandler(
            _auctionRepo.Object, _bidRepo.Object, _unitOfWork.Object,
            _notificationRepo.Object, _itemRepo.Object, _userRepo.Object);
    }

    private static Auction DutchAuction(int minutesElapsed = 10) => new()
    {
        Id = Guid.NewGuid(),
        ItemId = Guid.NewGuid(),
        Type = AuctionType.Dutch,
        Status = AuctionStatus.Active,
        StartingPrice = 200m,
        ReservePrice = 120m,    // donja granica
        BidIncrement = 5m,      // pad po minutu
        CurrentPrice = 200m,
        StartTime = DateTime.UtcNow.AddMinutes(-minutesElapsed).AddSeconds(-5),
        EndTime = DateTime.UtcNow.AddHours(1)
    };

    [Fact]
    public void Cena_pada_za_korak_svakog_minuta()
    {
        var auction = DutchAuction(minutesElapsed: 10);

        DutchPricing.CurrentPrice(auction, DateTime.UtcNow).Should().Be(150m);
    }

    [Fact]
    public void Cena_nikad_ne_pada_ispod_donje_granice()
    {
        var auction = DutchAuction(minutesElapsed: 1000);

        DutchPricing.CurrentPrice(auction, DateTime.UtcNow).Should().Be(120m);
    }

    [Fact]
    public void Pre_pocetka_cena_je_pocetna()
    {
        var auction = DutchAuction();
        auction.StartTime = DateTime.UtcNow.AddHours(1);

        DutchPricing.CurrentPrice(auction, DateTime.UtcNow).Should().Be(200m);
    }

    [Fact]
    public async Task Prvi_koji_prihvati_cenu_pobedjuje_i_aukcija_se_zavrsava()
    {
        var auction = DutchAuction(minutesElapsed: 10);
        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, Buyer, 150m, null), default);

        auction.CurrentWinnerId.Should().Be(Buyer);
        auction.CurrentPrice.Should().Be(150m);
        auction.EndTime.Should().BeOnOrBefore(DateTime.UtcNow);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ponuda_ispod_trenutne_cene_se_odbija()
    {
        var auction = DutchAuction(minutesElapsed: 10);
        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        Func<Task> act = () => _handler.Handle(new PlaceBidCommand(auction.Id, Buyer, 100m, null), default);

        await act.Should().ThrowAsync<ArgumentException>();
        auction.CurrentWinnerId.Should().BeNull();
    }

    [Fact]
    public async Task Neverifikovan_korisnik_ne_moze_da_licitira()
    {
        var auction = DutchAuction();
        _userRepo
            .Setup(r => r.GetByIdAsync(Buyer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Buyer, IsVerified = false, HasPaymentMethod = true });

        Func<Task> act = () => _handler.Handle(new PlaceBidCommand(auction.Id, Buyer, 200m, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Korisnik_bez_nacina_placanja_ne_moze_da_licitira()
    {
        var auction = DutchAuction();
        _userRepo
            .Setup(r => r.GetByIdAsync(Buyer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = Buyer, IsVerified = true, HasPaymentMethod = false });

        Func<Task> act = () => _handler.Handle(new PlaceBidCommand(auction.Id, Buyer, 200m, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
