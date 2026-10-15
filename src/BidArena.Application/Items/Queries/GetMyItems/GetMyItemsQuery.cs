using BidArena.Application.Common.Models;
using BidArena.Application.Items.Dtos;
using MediatR;

namespace BidArena.Application.Items.Queries.GetMyItems;

public record GetMyItemsQuery(Guid SellerId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ItemDto>>;
