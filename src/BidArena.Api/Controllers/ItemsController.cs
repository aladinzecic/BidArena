using BidArena.Api.Extensions;
using BidArena.Application.Items.Commands.AddItemPhoto;
using BidArena.Application.Items.Commands.CreateItem;
using BidArena.Application.Items.Commands.DeleteItem;
using BidArena.Application.Items.Commands.RemoveItemPhoto;
using BidArena.Application.Items.Commands.UpdateItem;
using BidArena.Application.Items.Queries.GetItemById;
using BidArena.Application.Items.Queries.GetMyItems;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BidArena.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ItemsController : ControllerBase
{
    private const int PhotoRequestLimitBytes = (int)AddItemPhotoCommand.MaxSizeBytes + 512 * 1024;

    private readonly IMediator _mediator;

    public ItemsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> CreateItem(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateItemCommand(
            User.GetUserId(),
            request.Title,
            request.DescriptionMarkdown,
            request.Category,
            request.Condition,
            request.Location);

        var itemId = await _mediator.Send(command, cancellationToken);
        return Ok(new { id = itemId });
    }

    // Predmeti prijavljenog prodavca (najnoviji prvo).
    [HttpGet("mine")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetMyItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetMyItemsQuery(User.GetUserId(), page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> GetItem(Guid id, CancellationToken cancellationToken)
    {
        var item = await _mediator.Send(new GetItemByIdQuery(id, User.GetUserId()), cancellationToken);
        return Ok(item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> UpdateItem(Guid id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateItemCommand(
            id,
            User.GetUserId(),
            request.Title,
            request.DescriptionMarkdown,
            request.Category,
            request.Condition,
            request.Location), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteItemCommand(id, User.GetUserId()), cancellationToken);
        return NoContent();
    }

    // multipart/form-data, polje "file" (JPEG, PNG ili WebP do 5 MB).
    [HttpPost("{id:guid}/photos")]
    [Authorize(Roles = "Seller")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(PhotoRequestLimitBytes)]
    public async Task<IActionResult> AddPhoto(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new ArgumentException("Nedostaje fajl (multipart polje \"file\")");

        await using var stream = file.OpenReadStream();

        var photo = await _mediator.Send(
            new AddItemPhotoCommand(id, User.GetUserId(), stream, file.ContentType, file.Length),
            cancellationToken);

        return Ok(photo);
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize(Roles = "Seller")]
    public async Task<IActionResult> RemovePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveItemPhotoCommand(id, photoId, User.GetUserId()), cancellationToken);
        return NoContent();
    }
}

public record CreateItemRequest(
    string Title,
    string DescriptionMarkdown,
    string Category,
    string Condition,
    string Location);

public record UpdateItemRequest(
    string Title,
    string DescriptionMarkdown,
    string Category,
    string Condition,
    string Location);
