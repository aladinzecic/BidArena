using BidArena.Api.Extensions;
using BidArena.Application.Admin.Commands.VerifyUser;
using BidArena.Application.Admin.Queries.GetAdminStats;
using BidArena.Application.Admin.Queries.GetUnverifiedUsers;
using BidArena.Application.Disputes.Commands.ResolveDispute;
using BidArena.Application.Disputes.Commands.TakeDispute;
using BidArena.Application.Disputes.Queries.GetDisputes;
using BidArena.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("disputes")]
    public async Task<IActionResult> GetDisputes([FromQuery] DisputeStatus? status, CancellationToken cancellationToken)
    {
        var disputes = await _mediator.Send(new GetDisputesQuery(status), cancellationToken);
        return Ok(disputes);
    }

    [HttpPost("disputes/{id:guid}/take")]
    public async Task<IActionResult> TakeDispute(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new TakeDisputeCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("disputes/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveDispute(
        Guid id, ResolveDisputeRequest request, CancellationToken cancellationToken)
    {
        var outcome = DisputeOutcome.FavorSeller;
        if (!string.IsNullOrWhiteSpace(request.Outcome) &&
            !Enum.TryParse(request.Outcome, true, out outcome))
            throw new ArgumentException("Nepoznat ishod spora, dozvoljeno: FavorSeller, RefundBuyer");

        await _mediator.Send(
            new ResolveDisputeCommand(id, User.GetUserId(), request.Resolution, outcome), cancellationToken);
        return NoContent();
    }

    [HttpGet("users/unverified")]
    public async Task<IActionResult> GetUnverifiedUsers(CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(new GetUnverifiedUsersQuery(), cancellationToken);
        return Ok(users);
    }

    [HttpPost("users/{id:guid}/verify")]
    public async Task<IActionResult> VerifyUser(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new VerifyUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _mediator.Send(new GetAdminStatsQuery(), cancellationToken);
        return Ok(stats);
    }
}

// Outcome: FavorSeller (podrazumevano) ili RefundBuyer.
public record ResolveDisputeRequest(string Resolution, string? Outcome = null);
