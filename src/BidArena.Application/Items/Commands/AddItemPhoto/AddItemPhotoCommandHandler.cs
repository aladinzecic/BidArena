using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Items.Dtos;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Items.Commands.AddItemPhoto;

public class AddItemPhotoCommandHandler : IRequestHandler<AddItemPhotoCommand, ItemPhotoDto>
{
    private readonly IItemRepository _itemRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IFileStorage _fileStorage;

    public AddItemPhotoCommandHandler(
        IItemRepository itemRepository,
        IAuctionRepository auctionRepository,
        IFileStorage fileStorage)
    {
        _itemRepository = itemRepository;
        _auctionRepository = auctionRepository;
        _fileStorage = fileStorage;
    }

    public async Task<ItemPhotoDto> Handle(AddItemPhotoCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete dodavati slike samo svojim predmetima");

        if (item.Photos.Count >= ItemRules.MaxPhotosPerItem)
            throw new InvalidOperationException($"Predmet može imati najviše {ItemRules.MaxPhotosPerItem} slika");

        await ItemRules.EnsureEditableAsync(_auctionRepository, item.Id, cancellationToken);

        var extension = AddItemPhotoCommand.AllowedContentTypes[request.ContentType];

        if (!ImageSignature.Matches(request.Content, request.ContentType))
            throw new ArgumentException("Sadržaj fajla ne odgovara navedenom formatu slike");

        var url = await _fileStorage.SaveAsync(item.Id.ToString("N"), extension, request.Content, cancellationToken);

        var photo = new ItemPhoto
        {
            Id = Guid.NewGuid(),
            ItemId = item.Id,
            Url = url,
            OrderIndex = item.Photos.Count == 0 ? 0 : item.Photos.Max(p => p.OrderIndex) + 1
        };

        try
        {
            await _itemRepository.AddPhotoAsync(photo, cancellationToken);
        }
        catch
        {
            // Da ne ostane "siroce" na disku ako upis u bazu nije uspeo.
            await _fileStorage.DeleteAsync(url, CancellationToken.None);
            throw;
        }

        return new ItemPhotoDto(photo.Id, photo.Url, photo.OrderIndex);
    }
}
