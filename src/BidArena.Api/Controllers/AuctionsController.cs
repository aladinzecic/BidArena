using BidArena.Api.Extensions;
using BidArena.Api.Middleware;
using BidArena.Application.Auctions.Commands.ActivateAuction;
using BidArena.Application.Auctions.Commands.CancelAuction;
using BidArena.Application.Auctions.Commands.CreateAuction;
using BidArena.Application.Auctions.Queries.GetAuctionById;
using BidArena.Application.Auctions.Queries.GetAuctions;
using BidArena.Application.Auctions.Queries.GetMyAuctions;
using BidArena.Application.Bids.Commands.PlaceBid;
using BidArena.Application.Bids.Queries.GetBidHistory;
using BidArena.Application.Disputes.Commands.OpenDispute;
using BidArena.Application.Ratings.Commands.CreateRating;
using BidArena.Application.Questions.Commands.AnswerQuestion;
using BidArena.Application.Questions.Commands.AskQuestion;
using BidArena.Application.Questions.Queries.GetAuctionQuestions;
using BidArena.Application.Shipments.Commands.ConfirmDelivery;
using BidArena.Application.Shipments.Commands.MarkShipped;
using BidArena.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuctionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] AuctionStatus? status,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAuctionsQuery(status, category, search, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    // Sve aukcije prodavca (i Draft), za upravljanje: aktivacija, otkazivanje, slanje.
    [HttpGet("mine")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var auctions = await _mediator.Send(new GetMyAuctionsQuery(User.GetUserId()), cancellationToken);
        return Ok(auctions);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAuction(Guid id, CancellationToken cancellationToken)
    {
        var auction = await _mediator.Send(
            new GetAuctionByIdQuery(id, User.TryGetUserId()),
            cancellationToken);

        return Ok(auction);
    }

    [HttpPost]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> CreateAuction(CreateAuctionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAuctionCommand(
            User.GetUserId(),
            request.ItemId,
            request.Type,
            request.StartingPrice,
            request.ReservePrice,
            request.BidIncrement,
            request.StartTime,
            request.EndTime);

        var auctionId = await _mediator.Send(command, cancellationToken);
        return Ok(new { id = auctionId });
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "Seller")]
    [AllowedPhases(AuctionStatus.Draft)]
    public async Task<IActionResult> ActivateAuction(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateAuctionCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    // Draft/Scheduled uvek; Active samo dok nema nijedne ponude.
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Seller")]
    [AllowedPhases(AuctionStatus.Draft, AuctionStatus.Scheduled, AuctionStatus.Active)]
    public async Task<IActionResult> CancelAuction(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelAuctionCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/bids")]
    [Authorize(Roles = "Bidder")]
    [AllowedPhases(AuctionStatus.Active)]
    public async Task<IActionResult> PlaceBid(Guid id, PlaceBidRequest request, CancellationToken cancellationToken)
    {
        var bidId = await _mediator.Send(
            new PlaceBidCommand(id, User.GetUserId(), request.Amount, request.ProxyMaxAmount),
            cancellationToken);

        return Ok(new { id = bidId });
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize(Roles = "Seller")]
    [AllowedPhases(AuctionStatus.Ended)]
    public async Task<IActionResult> Ship(Guid id, ShipRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new MarkShippedCommand(id, User.GetUserId(), request.TrackingNumber),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/confirm-delivery")]
    [Authorize(Roles = "Bidder")]
    [AllowedPhases(AuctionStatus.Ended)]
    public async Task<IActionResult> ConfirmDelivery(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmDeliveryCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/disputes")]
    [Authorize(Roles = "Seller,Bidder")]
    [AllowedPhases(AuctionStatus.Ended)]
    public async Task<IActionResult> OpenDispute(Guid id, OpenDisputeRequest request, CancellationToken cancellationToken)
    {
        var disputeId = await _mediator.Send(
            new OpenDisputeCommand(id, User.GetUserId(), request.Reason),
            cancellationToken);

        return Ok(new { id = disputeId });
    }

    // Ocena druge strane nakon uspešno završene aukcije (Settled).
    [HttpPost("{id:guid}/ratings")]
    [Authorize(Roles = "Seller,Bidder")]
    [AllowedPhases(AuctionStatus.Settled)]
    public async Task<IActionResult> Rate(Guid id, RateRequest request, CancellationToken cancellationToken)
    {
        var ratingId = await _mediator.Send(
            new CreateRatingCommand(id, User.GetUserId(), request.Score, request.Comment),
            cancellationToken);

        return Ok(new { id = ratingId });
    }

    // Pitanja i odgovori: pitanja vide svi, postavlja ih bilo ko prijavljen (osim prodavca),
    // a odgovara isključivo prodavac te aukcije.
    [HttpGet("{id:guid}/questions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQuestions(Guid id, CancellationToken cancellationToken)
    {
        var questions = await _mediator.Send(new GetAuctionQuestionsQuery(id), cancellationToken);
        return Ok(questions);
    }

    [HttpPost("{id:guid}/questions")]
    [Authorize(Roles = "Seller,Bidder")]
    public async Task<IActionResult> AskQuestion(Guid id, AskQuestionRequest request, CancellationToken cancellationToken)
    {
        var questionId = await _mediator.Send(
            new AskQuestionCommand(id, User.GetUserId(), request.Text),
            cancellationToken);

        return Ok(new { id = questionId });
    }

    [HttpPost("{id:guid}/questions/{questionId:guid}/answer")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> AnswerQuestion(
        Guid id, Guid questionId, AnswerQuestionRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new AnswerQuestionCommand(id, questionId, User.GetUserId(), request.Text),
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/bids")]
    public async Task<IActionResult> GetBidHistory(Guid id, CancellationToken cancellationToken)
    {
        var bids = await _mediator.Send(new GetBidHistoryQuery(id), cancellationToken);
        return Ok(bids);
    }
}

public record CreateAuctionRequest(
    Guid ItemId,
    string Type,
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal BidIncrement,
    DateTime StartTime,
    DateTime EndTime);

public record PlaceBidRequest(decimal Amount, decimal? ProxyMaxAmount);

public record ShipRequest(string TrackingNumber);

public record OpenDisputeRequest(string Reason);

public record RateRequest(int Score, string? Comment);

public record AskQuestionRequest(string Text);

public record AnswerQuestionRequest(string Text);