using BidArena.Application.Common.Interfaces;
using BidArena.Application.Users.Queries.GetUserRating;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Ratings;

public class GetUserRatingQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRatingRepository> _ratingRepo = new();
    private readonly GetUserRatingQueryHandler _handler;

    public GetUserRatingQueryHandlerTests()
    {
        _handler = new GetUserRatingQueryHandler(_userRepo.Object, _ratingRepo.Object);
    }

    [Fact]
    public async Task Vraca_javnu_reputaciju_bez_privatnih_podataka_i_bez_imena_ocenjivaca()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "tajna@example.com",
            DisplayName = "Marko",
            IsVerified = true,
            AverageRating = 4.5,
            RatingsCount = 2,
            SuccessfulAuctionsCount = 3,
            CancelledAuctionsCount = 1
        };
        var ratings = new List<Rating>
        {
            new() { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), RaterId = Guid.NewGuid(), RatedUserId = user.Id, Score = 5, Comment = "Odlično", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), AuctionId = Guid.NewGuid(), RaterId = Guid.NewGuid(), RatedUserId = user.Id, Score = 4, Comment = null, CreatedAt = DateTime.UtcNow.AddDays(-1) }
        };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ratingRepo.Setup(r => r.GetForUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ratings);

        var dto = await _handler.Handle(new GetUserRatingQuery(user.Id), default);

        dto.DisplayName.Should().Be("Marko");
        dto.IsVerified.Should().BeTrue();
        dto.AverageRating.Should().Be(4.5);
        dto.RatingsCount.Should().Be(2);
        dto.SuccessfulAuctionsCount.Should().Be(3);
        dto.CancelledAuctionsCount.Should().Be(1);
        dto.RecentRatings.Select(r => r.Score).Should().Equal(5, 4);
        dto.RecentRatings[0].Comment.Should().Be("Odlično");
        dto.RecentRatings[1].Comment.Should().BeNull();
    }

    [Fact]
    public async Task Korisnik_bez_ocena_vraca_praznu_listu()
    {
        var user = new User { Id = Guid.NewGuid(), DisplayName = "Nova" };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ratingRepo.Setup(r => r.GetForUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Rating>());

        var dto = await _handler.Handle(new GetUserRatingQuery(user.Id), default);

        dto.RatingsCount.Should().Be(0);
        dto.RecentRatings.Should().BeEmpty();
    }

    [Fact]
    public async Task Nepostojeci_korisnik_baca_KeyNotFound()
    {
        var act = () => _handler.Handle(new GetUserRatingQuery(Guid.NewGuid()), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
