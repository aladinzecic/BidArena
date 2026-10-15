using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Guid>
{
    private readonly IItemRepository _itemRepository;

    public CreateItemCommandHandler(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<Guid> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ItemCondition>(request.Condition, true, out var condition))
        {
            throw new ArgumentException($"Nepoznato stanje predmeta: {request.Condition}");
        }

        if (!ItemCategories.TryParse(request.Category, out var category))
        {
            throw new ArgumentException($"Nepoznata kategorija: {request.Category}");
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            SellerId = request.SellerId,
            Title = request.Title,
            DescriptionMarkdown = request.DescriptionMarkdown,
            Category = category,
            Condition = condition,
            Location = request.Location
        };

        var createdItem = await _itemRepository.AddAsync(item, cancellationToken);

        return createdItem.Id;
    }
}