using BidArena.Application.Disputes.Dtos;
using MediatR;

namespace BidArena.Application.Disputes.Commands.AddDisputePhoto;

public record AddDisputePhotoCommand(
    Guid DisputeId,
    Guid UserId,
    Stream Content,
    string ContentType,
    long Length
) : IRequest<DisputePhotoDto>
{
    public const int MaxPhotosPerDispute = 5;
}
