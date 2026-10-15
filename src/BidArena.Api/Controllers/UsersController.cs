using BidArena.Api.Extensions;
using BidArena.Application.Users.Commands.AddPaymentMethod;
using BidArena.Application.Users.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BidArena.Application.Users.Queries.GetUserRating;
namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Profil prijavljenog korisnika (verifikacija, način plaćanja, ocena).
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var profile = await _mediator.Send(new GetMyProfileQuery(User.GetUserId()), cancellationToken);
        return Ok(profile);
    }

    // Javna reputacija bilo kog korisnika: prosečna ocena, broj ocena, uspešne/otkazane aukcije i poslednje ocene.
    [HttpGet("{id:guid}/rating")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRating(Guid id, CancellationToken cancellationToken)
    {
        var rating = await _mediator.Send(new GetUserRatingQuery(id), cancellationToken);
        return Ok(rating);
    }

    // Simulacija dodavanja načina plaćanja (preduslov za nadmetanje).
    [HttpPost("me/payment-method")]
    public async Task<IActionResult> AddPaymentMethod(CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddPaymentMethodCommand(User.GetUserId()), cancellationToken);
        return NoContent();
    }
}
