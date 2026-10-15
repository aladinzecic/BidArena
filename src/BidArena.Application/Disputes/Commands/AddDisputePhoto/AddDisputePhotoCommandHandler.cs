using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Dtos;
using BidArena.Application.Items.Commands.AddItemPhoto;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Disputes.Commands.AddDisputePhoto;

public class AddDisputePhotoCommandHandler : IRequestHandler<AddDisputePhotoCommand, DisputePhotoDto>
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IFileStorage _fileStorage;

    public AddDisputePhotoCommandHandler(IDisputeRepository disputeRepository, IFileStorage fileStorage)
    {
        _disputeRepository = disputeRepository;
        _fileStorage = fileStorage;
    }

    public async Task<DisputePhotoDto> Handle(AddDisputePhotoCommand request, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(request.DisputeId, cancellationToken)
            ?? throw new KeyNotFoundException($"Spor {request.DisputeId} ne postoji");

        if (dispute.OpenedByUserId != request.UserId)
            throw new ForbiddenAccessException("Slike može dodavati samo korisnik koji je otvorio spor");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new InvalidOperationException("Spor je već rešen, nije moguće dodati slike");

        if (dispute.Photos.Count >= AddDisputePhotoCommand.MaxPhotosPerDispute)
            throw new InvalidOperationException(
                $"Spor može imati najviše {AddDisputePhotoCommand.MaxPhotosPerDispute} slika");

        var extension = AddItemPhotoCommand.AllowedContentTypes[request.ContentType];

        if (!ImageSignature.Matches(request.Content, request.ContentType))
            throw new ArgumentException("Sadržaj fajla ne odgovara navedenom formatu slike");

        var url = await _fileStorage.SaveAsync(
            $"disputes/{dispute.Id:N}", extension, request.Content, cancellationToken);

        var photo = new DisputePhoto
        {
            Id = Guid.NewGuid(),
            DisputeId = dispute.Id,
            Url = url,
            OrderIndex = dispute.Photos.Count == 0 ? 0 : dispute.Photos.Max(p => p.OrderIndex) + 1
        };

        try
        {
            await _disputeRepository.AddPhotoAsync(photo, cancellationToken);
        }
        catch
        {
            // Da ne ostane "siroce" na disku ako upis u bazu nije uspeo.
            await _fileStorage.DeleteAsync(url, CancellationToken.None);
            throw;
        }

        return new DisputePhotoDto(photo.Id, photo.Url, photo.OrderIndex);
    }
}
