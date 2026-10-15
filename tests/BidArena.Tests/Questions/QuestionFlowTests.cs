using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Questions.Commands.AnswerQuestion;
using BidArena.Application.Questions.Commands.AskQuestion;
using BidArena.Application.Questions.Queries.GetAuctionQuestions;
using BidArena.Domain.Entities;
using FluentAssertions;
using Moq;

namespace BidArena.Tests.Questions;

public class QuestionFlowTests
{
    private static readonly Guid SellerId = Guid.NewGuid();
    private static readonly Guid AskerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly Mock<IAuctionRepository> _auctionRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<IQuestionRepository> _questionRepo = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly List<Question> _addedQuestions = new();
    private readonly List<Notification> _notifications = new();

    private readonly AskQuestionCommandHandler _askHandler;
    private readonly AnswerQuestionCommandHandler _answerHandler;

    public QuestionFlowTests()
    {
        _questionRepo
            .Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .Callback<Question, CancellationToken>((q, _) => _addedQuestions.Add(q))
            .Returns(Task.CompletedTask);

        _notificationRepo
            .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notifications.Add(n))
            .Returns(Task.CompletedTask);

        _askHandler = new AskQuestionCommandHandler(
            _auctionRepo.Object, _questionRepo.Object, _notificationRepo.Object,
            _unitOfWork.Object);

        _answerHandler = new AnswerQuestionCommandHandler(
            _auctionRepo.Object, _itemRepo.Object, _questionRepo.Object,
            _notificationRepo.Object, _unitOfWork.Object);
    }

    private Auction SetupAuction(AuctionStatus status = AuctionStatus.Active)
    {
        var item = new Item { Id = Guid.NewGuid(), SellerId = SellerId, Title = "Sat" };
        var auction = new Auction { Id = Guid.NewGuid(), ItemId = item.Id, Status = status };

        _auctionRepo
            .Setup(r => r.GetWithItemAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuctionWithItem(auction, item, 0, null));
        _auctionRepo
            .Setup(r => r.GetByIdAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);
        _auctionRepo
            .Setup(r => r.ExistsAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _itemRepo
            .Setup(r => r.GetByIdAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        return auction;
    }

    private Question SetupQuestion(Auction auction, string? answer = null)
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            AuctionId = auction.Id,
            AskedByUserId = AskerId,
            QuestionText = "Da li radi?",
            AnswerText = answer
        };
        _questionRepo
            .Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);
        return question;
    }

    // ---------- Postavljanje pitanja ----------

    [Fact]
    public async Task Ask_OnActiveAuction_SavesQuestionAndNotifiesSeller()
    {
        var auction = SetupAuction();

        var id = await _askHandler.Handle(
            new AskQuestionCommand(auction.Id, AskerId, "  Da li sat ima kutiju?  "), CancellationToken.None);

        _addedQuestions.Should().ContainSingle();
        var q = _addedQuestions[0];
        q.Id.Should().Be(id);
        q.AuctionId.Should().Be(auction.Id);
        q.AskedByUserId.Should().Be(AskerId);
        q.QuestionText.Should().Be("Da li sat ima kutiju?");
        q.AnswerText.Should().BeNull();

        _notifications.Should().ContainSingle(n =>
            n.UserId == SellerId && n.Type == NotificationType.QuestionAsked && n.AuctionId == auction.Id);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ask_OnScheduledAuction_IsAllowed()
    {
        var auction = SetupAuction(AuctionStatus.Scheduled);

        await _askHandler.Handle(new AskQuestionCommand(auction.Id, AskerId, "Koliko je star?"), CancellationToken.None);

        _addedQuestions.Should().ContainSingle();
    }

    [Fact]
    public async Task Ask_BySeller_ThrowsForbidden()
    {
        var auction = SetupAuction();

        var act = () => _askHandler.Handle(
            new AskQuestionCommand(auction.Id, SellerId, "Pitam sam sebe"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _addedQuestions.Should().BeEmpty();
    }

    [Theory]
    [InlineData(AuctionStatus.Draft)]
    [InlineData(AuctionStatus.Ended)]
    [InlineData(AuctionStatus.Settled)]
    [InlineData(AuctionStatus.Cancelled)]
    public async Task Ask_WhenAuctionNotScheduledOrActive_Throws(AuctionStatus status)
    {
        var auction = SetupAuction(status);

        var act = () => _askHandler.Handle(
            new AskQuestionCommand(auction.Id, AskerId, "Da li radi?"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _addedQuestions.Should().BeEmpty();
    }

    [Fact]
    public async Task Ask_UnknownAuction_ThrowsNotFound()
    {
        var act = () => _askHandler.Handle(
            new AskQuestionCommand(Guid.NewGuid(), AskerId, "Da li radi?"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ---------- Odgovaranje (samo prodavac) ----------

    [Fact]
    public async Task Answer_BySeller_SavesAnswerAndNotifiesAsker()
    {
        var auction = SetupAuction();
        var question = SetupQuestion(auction);

        await _answerHandler.Handle(
            new AnswerQuestionCommand(auction.Id, question.Id, SellerId, "  Da, ima kutiju.  "),
            CancellationToken.None);

        question.AnswerText.Should().Be("Da, ima kutiju.");
        question.AnsweredAt.Should().NotBeNull();
        _notifications.Should().ContainSingle(n =>
            n.UserId == AskerId && n.Type == NotificationType.QuestionAnswered);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]   // korisnik koji je postavio pitanje
    [InlineData(false)]  // neki treći korisnik
    public async Task Answer_ByNonSeller_ThrowsForbidden(bool byAsker)
    {
        var auction = SetupAuction();
        var question = SetupQuestion(auction);
        var requester = byAsker ? AskerId : OtherUserId;

        var act = () => _answerHandler.Handle(
            new AnswerQuestionCommand(auction.Id, question.Id, requester, "Pokušaj odgovora"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        question.AnswerText.Should().BeNull();
        _notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Answer_AlreadyAnswered_Throws()
    {
        var auction = SetupAuction();
        var question = SetupQuestion(auction, answer: "Već odgovoreno");

        var act = () => _answerHandler.Handle(
            new AnswerQuestionCommand(auction.Id, question.Id, SellerId, "Novi odgovor"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        question.AnswerText.Should().Be("Već odgovoreno");
    }

    [Fact]
    public async Task Answer_QuestionFromDifferentAuction_ThrowsNotFound()
    {
        var auction = SetupAuction();
        var question = SetupQuestion(auction);

        var act = () => _answerHandler.Handle(
            new AnswerQuestionCommand(Guid.NewGuid(), question.Id, SellerId, "Odgovor"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Answer_UnknownQuestion_ThrowsNotFound()
    {
        var auction = SetupAuction();

        var act = () => _answerHandler.Handle(
            new AnswerQuestionCommand(auction.Id, Guid.NewGuid(), SellerId, "Odgovor"),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ---------- Listanje ----------

    [Fact]
    public async Task GetQuestions_UnknownAuction_ThrowsNotFound()
    {
        var handler = new GetAuctionQuestionsQueryHandler(_auctionRepo.Object, _questionRepo.Object);

        var act = () => handler.Handle(new GetAuctionQuestionsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetQuestions_ReturnsQuestionsOfAuction()
    {
        var auction = SetupAuction();
        _questionRepo
            .Setup(r => r.GetByAuctionAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BidArena.Application.Questions.Dtos.QuestionDto>
            {
                new() { Id = Guid.NewGuid(), AuctionId = auction.Id, QuestionText = "Pitanje" }
            });
        var handler = new GetAuctionQuestionsQueryHandler(_auctionRepo.Object, _questionRepo.Object);

        var result = await handler.Handle(new GetAuctionQuestionsQuery(auction.Id), CancellationToken.None);

        result.Should().ContainSingle();
    }
}
