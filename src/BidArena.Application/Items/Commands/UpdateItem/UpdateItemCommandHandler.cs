using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IAuctionRepository _auctionRepository;

    public UpdateItemCommandHandler(IItemRepository itemRepository, IAuctionRepository auctionRepository)
    {
        _itemRepository = itemRepository;
        _auctionRepository = auctionRepository;
    }

    public async Task Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete menjati samo svoje predmete");

        if (!Enum.TryParse<ItemCondition>(request.Condition, true, out var condition))
            throw new ArgumentException($"Nepoznato stanje predmeta: {request.Condition}");

        if (!ItemCategories.TryParse(request.Category, out var category))
            throw new ArgumentException($"Nepoznata kategorija: {request.Category}");

        await ItemRules.EnsureEditableAsync(_auctionRepository, item.Id, cancellationToken);

        item.Title = request.Title;
        item.DescriptionMarkdown = request.DescriptionMarkdown;
        item.Category = category;
        item.Condition = condition;
        item.Location = request.Location;

        await _itemRepository.UpdateAsync(item, cancellationToken);
    }
}
