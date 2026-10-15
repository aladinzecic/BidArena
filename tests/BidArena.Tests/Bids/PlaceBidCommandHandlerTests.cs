using BidArena.Application.Bids.Commands.PlaceBid;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Bids;

public class PlaceBidCommandHandlerTests
{
    private static readonly Guid BidderA = Guid.NewGuid();
    private static readonly Guid BidderB = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IBidRepository> _bidRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly List<Notification> _notifications = new();
    private readonly List<Bid> _savedBids = new();
    private readonly PlaceBidCommandHandler _handler;

    public PlaceBidCommandHandlerTests()
    {
        _bidRepo
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Bid>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Bid>, CancellationToken>((bids, _) => _savedBids.AddRange(bids))
            .Returns(Task.CompletedTask);

        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        // Svi ponuđači u testovima su verifikovani i imaju način plaćanja.
        _userRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new User { Id = id, IsVerified = true, HasPaymentMethod = true });

        _handler = new PlaceBidCommandHandler(
            _auctionRepo.Object, _bidRepo.Object, _unitOfWork.Object,
            _notificationRepo.Object, _itemRepo.Object, _userRepo.Object);
    }

    // ---------- helperi ----------

    private static Auction CreateAuction(
        Guid? winnerId = null,
        decimal currentPrice = 100m,
        AuctionStatus status = AuctionStatus.Active,
        DateTime? endTime = null) => new()
    {
        Id = Guid.NewGuid(),
        Status = status,
        StartingPrice = 100m,
        BidIncrement = 10m,
        CurrentPrice = currentPrice,
        CurrentWinnerId = winnerId,
        StartTime = DateTime.UtcNow.AddHours(-1),
        EndTime = endTime ?? DateTime.UtcNow.AddHours(2),
        AntiSnipeWindowMinutes = 5,
        AntiSnipeExtensionMinutes = 10
    };

    private void SetupAuction(Auction auction) =>
        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

    private void SetupLeaderMax(Auction auction, Guid leaderId, decimal max) =>
        _bidRepo
            .Setup(r => r.GetMaxAmountForBidderAsync(auction.Id, leaderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)max);

    // ---------- obaveštenja i vlasnik ----------

    [Fact]
    public async Task Prodavac_ne_moze_da_se_nadmece_na_sopstvenoj_aukciji()
    {
        var auction = CreateAuction();
        SetupAuction(auction);
        _itemRepo
            .Setup(r => r.GetByIdAsync(auction.ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Item { Id = auction.ItemId, SellerId = BidderA, Title = "Sat" });

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _savedBids.Should().BeEmpty();
    }

    [Fact]
    public async Task Nadmasen_lider_dobija_Outbid_obavestenje()
    {
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 100m);
        SetupAuction(auction);
        SetupLeaderMax(auction, BidderA, 100m);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderB, 110m, null), default);

        auction.CurrentWinnerId.Should().Be(BidderB);
        _notifications.Should().ContainSingle(n => n.UserId == BidderA && n.Type == NotificationType.Outbid);
    }

    [Fact]
    public async Task Prva_ponuda_ne_salje_Outbid_obavestenje()
    {
        var auction = CreateAuction();
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        _notifications.Should().BeEmpty();
    }

    // ---------- obaveštenja i anti-snipe ----------



    [Fact]
    public async Task Ponuda_u_anti_snipe_prozoru_produzava_aukciju()
    {
        var endTime = DateTime.UtcNow.AddMinutes(2);
        var auction = CreateAuction(endTime: endTime);
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        auction.EndTime.Should().Be(endTime.AddMinutes(auction.AntiSnipeExtensionMinutes));
    }

    [Fact]
    public async Task Odbijena_ponuda_ne_pravi_obavestenja()
    {
        var auction = CreateAuction(status: AuctionStatus.Ended);
        SetupAuction(auction);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _notifications.Should().BeEmpty();
    }

    // ---------- osnovna pravila ----------

    [Fact]
    public async Task Prva_ponuda_postavlja_cenu_i_pobednika()
    {
        var auction = CreateAuction();
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        auction.CurrentPrice.Should().Be(100m);
        auction.CurrentWinnerId.Should().Be(BidderA);
        _savedBids.Should().ContainSingle();
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aukcija_ne_postoji_baca_KeyNotFound_i_radi_rollback()
    {
        var id = Guid.NewGuid();
        _auctionRepo
            .Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Auction?)null);

        var act = () => _handler.Handle(new PlaceBidCommand(id, BidderA, 100m, null), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aukcija_koja_nije_Active_baca_InvalidOperation()
    {
        var auction = CreateAuction(status: AuctionStatus.Draft);
        SetupAuction(auction);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _savedBids.Should().BeEmpty();
    }

    [Fact]
    public async Task Ponuda_posle_isteka_aukcije_baca_InvalidOperation()
    {
        var auction = CreateAuction(endTime: DateTime.UtcNow.AddMinutes(-1));
        SetupAuction(auction);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Trenutni_lider_ne_moze_da_nadmece_sam_sebe()
    {
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 100m);
        SetupAuction(auction);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 200m, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Ponuda_ispod_minimuma_baca_ArgumentException_i_radi_rollback()
    {
        // minimum = 100 + 10 = 110
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 100m);
        SetupAuction(auction);
        SetupLeaderMax(auction, BidderA, 100m);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderB, 105m, null), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Proxy_maksimum_manji_od_iznosa_baca_ArgumentException_pre_transakcije()
    {
        var auction = CreateAuction();
        SetupAuction(auction);

        var act = () => _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 150m, 120m), default);

        await act.Should().ThrowAsync<ArgumentException>();
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- proxy bid ----------

    [Fact]
    public async Task Novi_bidder_sa_vecim_maksimumom_pobedjuje_i_placa_maksimum_lidera_plus_korak()
    {
        // Lider A: max 200, cena 110. B nudi 120 sa maksimumom 300.
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 110m);
        SetupAuction(auction);
        SetupLeaderMax(auction, BidderA, 200m);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderB, 120m, 300m), default);

        auction.CurrentWinnerId.Should().Be(BidderB);
        auction.CurrentPrice.Should().Be(210m); // min(300, 200 + 10)

        // Istorija: B 120, A 200 (proxy), B 210 (proxy)
        _savedBids.Should().HaveCount(3);
        _savedBids.Should().Contain(b => b.BidderId == BidderA && b.Amount == 200m && b.IsProxy);
        _savedBids.Should().Contain(b => b.BidderId == BidderB && b.Amount == 210m && b.IsProxy);
    }

    [Fact]
    public async Task Lider_sa_vecim_maksimumom_zadrzava_vodjstvo()
    {
        // Lider A: max 200, cena 110. B nudi 120 sa maksimumom 150.
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 110m);
        SetupAuction(auction);
        SetupLeaderMax(auction, BidderA, 200m);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderB, 120m, 150m), default);

        auction.CurrentWinnerId.Should().Be(BidderA);
        auction.CurrentPrice.Should().Be(160m); // min(200, 150 + 10)
        _savedBids.Should().Contain(b => b.BidderId == BidderA && b.Amount == 160m && b.IsProxy);
    }

    [Fact]
    public async Task Kod_izjednacenja_maksimuma_pobedjuje_raniji_lider()
    {
        var auction = CreateAuction(winnerId: BidderA, currentPrice: 110m);
        SetupAuction(auction);
        SetupLeaderMax(auction, BidderA, 200m);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderB, 120m, 200m), default);

        auction.CurrentWinnerId.Should().Be(BidderA);
        auction.CurrentPrice.Should().Be(200m); // min(200, 200 + 10)
    }

    [Fact]
    public async Task Tajni_maksimum_se_cuva_samo_u_bidu_koji_ga_je_postavio()
    {
        var auction = CreateAuction();
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, 500m), default);

        _savedBids.Single().ProxyMaxAmount.Should().Be(500m);
    }

    // ---------- anti-snipe ----------

    [Fact]
    public async Task Ponuda_u_anti_snipe_prozoru_produzava_kraj_aukcije()
    {
        var originalEnd = DateTime.UtcNow.AddMinutes(3); // unutar prozora od 5 min
        var auction = CreateAuction(endTime: originalEnd);
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        auction.EndTime.Should().Be(originalEnd.AddMinutes(10));
    }

    [Fact]
    public async Task Ponuda_van_anti_snipe_prozora_ne_menja_kraj_aukcije()
    {
        var originalEnd = DateTime.UtcNow.AddHours(2);
        var auction = CreateAuction(endTime: originalEnd);
        SetupAuction(auction);

        await _handler.Handle(new PlaceBidCommand(auction.Id, BidderA, 100m, null), default);

        auction.EndTime.Should().Be(originalEnd);
    }
}