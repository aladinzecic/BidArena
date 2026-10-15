using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Ratings.Commands.CreateRating;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Ratings;

public class CreateRatingCommandHandlerTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid BuyerId = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IRatingRepository> _ratingRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<Rating> _ratings = new();
    private readonly CreateRatingCommandHandler _handler;

    public CreateRatingCommandHandlerTests()
    {
        _ratingRepo
            .Setup(r => r.AddAsync(It.IsAny<Rating>(), It.IsAny<CancellationToken>()))
            .Callback<Rating, CancellationToken>((r, _) => _ratings.Add(r))
            .Returns(Task.CompletedTask);

        _handler = new CreateRatingCommandHandler(
            _auctionRepo.Object, _ratingRepo.Object, _userRepo.Object, _unitOfWork.Object);
    }

    private Auction Setup(AuctionStatus status = AuctionStatus.Settled)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Status = status,
            CurrentWinnerId = BuyerId
        };

        _auctionRepo
            .Setup(r => r.GetWithItemAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuctionWithItem(auction, item, 3, null));

        return auction;
    }

    private User SetupUser(Guid id, double average = 0, int count = 0)
    {
        var user = new User { Id = id, AverageRating = average, RatingsCount = count };
        _userRepo
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Kupac_ocenjuje_prodavca_i_prosek_se_azurira()
    {
        var auction = Setup();
        var seller = SetupUser(SellerId, average: 4.0, count: 1);

        await _handler.Handle(new CreateRatingCommand(auction.Id, BuyerId, 5, "  Odlično  "), default);

        var rating = _ratings.Should().ContainSingle().Subject;
        rating.RatedUserId.Should().Be(SellerId);
        rating.RaterId.Should().Be(BuyerId);
        rating.Comment.Should().Be("Odlično");
        seller.RatingsCount.Should().Be(2);
        seller.AverageRating.Should().Be(4.5);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Prodavac_ocenjuje_kupca()
    {
        var auction = Setup();
        var buyer = SetupUser(BuyerId);

        await _handler.Handle(new CreateRatingCommand(auction.Id, SellerId, 3, null), default);

        _ratings.Should().ContainSingle(r => r.RatedUserId == BuyerId);
        buyer.AverageRating.Should().Be(3.0);
        buyer.RatingsCount.Should().Be(1);
    }

    [Fact]
    public async Task Ocenjivanje_pre_Settled_statusa_nije_dozvoljeno()
    {
        var auction = Setup(AuctionStatus.Ended);

        Func<Task> act = () => _handler.Handle(new CreateRatingCommand(auction.Id, BuyerId, 5, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _ratings.Should().BeEmpty();
    }

    [Fact]
    public async Task Treca_strana_ne_moze_da_ocenjuje()
    {
        var auction = Setup();

        Func<Task> act = () => _handler.Handle(new CreateRatingCommand(auction.Id, Guid.NewGuid(), 5, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Dupla_ocena_iste_aukcije_nije_dozvoljena()
    {
        var auction = Setup();
        _ratingRepo
            .Setup(r => r.ExistsAsync(auction.Id, BuyerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => _handler.Handle(new CreateRatingCommand(auction.Id, BuyerId, 5, null), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _ratings.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Ocena_van_opsega_nije_validna(int score)
    {
        var result = new CreateRatingCommandValidator()
            .Validate(new CreateRatingCommand(Guid.NewGuid(), BuyerId, score, null));

        result.IsValid.Should().BeFalse();
    }
}
