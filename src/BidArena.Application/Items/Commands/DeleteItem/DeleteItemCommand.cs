using MediatR;

namespace BidArena.Application.Items.Commands.DeleteItem;

public record DeleteItemCommand(Guid ItemId, Guid SellerId) : IRequest;
