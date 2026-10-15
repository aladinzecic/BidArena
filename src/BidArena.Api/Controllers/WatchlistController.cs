using BidArena.Api.Extensions;
using BidArena.Application.Watchlist.Commands.AddToWatchlist;
using BidArena.Application.Watchlist.Commands.RemoveFromWatchlist;
using BidArena.Application.Watchlist.Queries.GetMyWatchlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WatchlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var watchlist = await _mediator.Send(new GetMyWatchlistQuery(User.GetUserId()), cancellationToken);
        return Ok(watchlist);
    }

    [HttpPost("{auctionId:guid}")]
    public async Task<IActionResult> Add(Guid auctionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddToWatchlistCommand(User.GetUserId(), auctionId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{auctionId:guid}")]
    public async Task<IActionResult> Remove(Guid auctionId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveFromWatchlistCommand(User.GetUserId(), auctionId), cancellationToken);
        return NoContent();
    }
}
