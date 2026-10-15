using MediatR;

namespace BidArena.Application.Items.Commands.UpdateItem;

public record UpdateItemCommand(
    Guid ItemId,
    Guid SellerId,
    string Title,
    string DescriptionMarkdown,
    string Category,
    string Condition,
    string Location
) : IRequest;
