using MediatR;

namespace BidArena.Application.Items.Commands.CreateItem;

public record CreateItemCommand(
    Guid SellerId,
    string Title,
    string DescriptionMarkdown,
    string Category,
    string Condition,
    string Location
) : IRequest<Guid>;