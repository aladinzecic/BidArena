using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Items.Commands.RemoveItemPhoto;

public class RemoveItemPhotoCommandHandler : IRequestHandler<RemoveItemPhotoCommand>
{
    private readonly IItemRepository _itemRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IFileStorage _fileStorage;

    public RemoveItemPhotoCommandHandler(
        IItemRepository itemRepository,
        IAuctionRepository auctionRepository,
        IFileStorage fileStorage)
    {
        _itemRepository = itemRepository;
        _auctionRepository = auctionRepository;
        _fileStorage = fileStorage;
    }

    public async Task Handle(RemoveItemPhotoCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete brisati slike samo svojih predmeta");

        var photo = item.Photos.FirstOrDefault(p => p.Id == request.PhotoId)
            ?? throw new KeyNotFoundException($"Slika {request.PhotoId} ne postoji");

        await ItemRules.EnsureEditableAsync(_auctionRepository, item.Id, cancellationToken);

        var url = photo.Url;
        await _itemRepository.RemovePhotoAsync(photo, cancellationToken);
        await _fileStorage.DeleteAsync(url, cancellationToken);
    }
}
