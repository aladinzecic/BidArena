using BidArena.Api.Extensions;
using BidArena.Application.Notifications.Commands.MarkNotificationRead;
using BidArena.Application.Notifications.Queries.GetMyNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var notifications = await _mediator.Send(new GetMyNotificationsQuery(User.GetUserId()), cancellationToken);
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}
