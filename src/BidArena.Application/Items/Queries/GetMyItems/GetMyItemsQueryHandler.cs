using BidArena.Application.Common.Interfaces;
using BidArena.Application.Common.Models;
using BidArena.Application.Items.Dtos;
using MediatR;

namespace BidArena.Application.Items.Queries.GetMyItems;

public class GetMyItemsQueryHandler : IRequestHandler<GetMyItemsQuery, PagedResult<ItemDto>>
{
    private readonly IItemRepository _itemRepository;

    public GetMyItemsQueryHandler(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<PagedResult<ItemDto>> Handle(GetMyItemsQuery request, CancellationToken cancellationToken)
    {
        var page = await _itemRepository.GetBySellerAsync(
            request.SellerId, request.Page, request.PageSize, cancellationToken);

        return new PagedResult<ItemDto>(
            page.Items.Select(ItemDto.From).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize);
    }
}
