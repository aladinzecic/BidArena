using BidArena.Api.Extensions;
using BidArena.Application.Disputes.Commands.AddDisputePhoto;
using BidArena.Application.Items.Commands.AddItemPhoto;
using BidArena.Application.Disputes.Queries.GetMyDisputes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DisputesController : ControllerBase
{
    private const int PhotoRequestLimitBytes = (int)AddItemPhotoCommand.MaxSizeBytes + 512 * 1024;

    private readonly IMediator _mediator;

    public DisputesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Sporovi koje je korisnik otvorio ili u kojima je druga strana.
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var disputes = await _mediator.Send(new GetMyDisputesQuery(User.GetUserId()), cancellationToken);
        return Ok(disputes);
    }

    // Dodavanje slike (dokaza) uz spor: samo korisnik koji je otvorio spor, dok spor nije rešen.
    // multipart/form-data, polje "file" (JPEG, PNG ili WebP do 5 MB, najviše 5 slika po sporu).
    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PhotoRequestLimitBytes)]
    public async Task<IActionResult> AddPhoto(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new ArgumentException("Nedostaje fajl (multipart polje \"file\")");

        await using var stream = file.OpenReadStream();

        var photo = await _mediator.Send(
            new AddDisputePhotoCommand(id, User.GetUserId(), stream, file.ContentType, file.Length),
            cancellationToken);

        return Ok(photo);
    }
}
