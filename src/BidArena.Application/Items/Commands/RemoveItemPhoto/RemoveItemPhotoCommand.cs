using MediatR;

namespace BidArena.Application.Items.Commands.RemoveItemPhoto;

public record RemoveItemPhotoCommand(Guid ItemId, Guid PhotoId, Guid SellerId) : IRequest;
