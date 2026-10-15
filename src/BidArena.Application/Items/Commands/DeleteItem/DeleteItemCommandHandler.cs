using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Items.Commands.DeleteItem;

public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IFileStorage _fileStorage;

    public DeleteItemCommandHandler(
        IItemRepository itemRepository,
        IAuctionRepository auctionRepository,
        IFileStorage fileStorage)
    {
        _itemRepository = itemRepository;
        _auctionRepository = auctionRepository;
        _fileStorage = fileStorage;
    }

    public async Task Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete brisati samo svoje predmete");

        var statuses = await _auctionRepository.GetStatusesByItemIdAsync(item.Id, cancellationToken);
        if (statuses.Count > 0)
            throw new InvalidOperationException("Predmet ima aukcije i ne može se obrisati");

        var photoUrls = item.Photos.Select(p => p.Url).ToList();

        await _itemRepository.DeleteAsync(item, cancellationToken);

        // Fajlovi se brisu tek kada je red u bazi obrisan.
        foreach (var url in photoUrls)
            await _fileStorage.DeleteAsync(url, cancellationToken);
    }
}
