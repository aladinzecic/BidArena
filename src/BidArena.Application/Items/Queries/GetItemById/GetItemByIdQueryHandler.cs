using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Items.Dtos;
using MediatR;

namespace BidArena.Application.Items.Queries.GetItemById;

public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, ItemDto>
{
    private readonly IItemRepository _itemRepository;

    public GetItemByIdQueryHandler(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<ItemDto> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete pregledati samo svoje predmete");

        return ItemDto.From(item);
    }
}
