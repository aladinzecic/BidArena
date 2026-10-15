using BidArena.Application.Items.Dtos;
using MediatR;

namespace BidArena.Application.Items.Queries.GetItemById;

public record GetItemByIdQuery(Guid ItemId, Guid SellerId) : IRequest<ItemDto>;
