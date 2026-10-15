using BidArena.Api.Extensions;
using BidArena.Application.Shipments.Queries.GetMyShipments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShipmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Pošiljke u kojima je korisnik prodavac ili kupac.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var shipments = await _mediator.Send(new GetMyShipmentsQuery(User.GetUserId()), cancellationToken);
        return Ok(shipments);
    }
}
