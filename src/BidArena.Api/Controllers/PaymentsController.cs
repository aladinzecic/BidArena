using BidArena.Api.Extensions;
using BidArena.Application.Payments.Commands.PayPayment;
using BidArena.Application.Payments.Queries.GetMyPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Bidder")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var payments = await _mediator.Send(new GetMyPaymentsQuery(User.GetUserId()), cancellationToken);
        return Ok(payments);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PayPaymentCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}