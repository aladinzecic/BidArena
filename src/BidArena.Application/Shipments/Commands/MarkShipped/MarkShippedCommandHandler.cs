using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Shipments.Commands.MarkShipped;

public class MarkShippedCommandHandler : IRequestHandler<MarkShippedCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkShippedCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IShipmentRepository shipmentRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _shipmentRepository = shipmentRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkShippedCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Redosled zaključavanja je uvek: aukcija pa pošiljka (izbegava deadlock).
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

            if (item.SellerId != request.SellerId)
                throw new ForbiddenAccessException("Možete slati samo sopstvene predmete");

            if (auction.Status != AuctionStatus.Ended)
                throw new InvalidOperationException(
                    $"Pošiljka se može poslati samo za aukciju u statusu Ended, trenutni status je {auction.Status}");

            var shipment = await _shipmentRepository.GetByAuctionIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException("Pošiljka ne postoji, kupac još nije platio");

            if (shipment.ShippedAt is not null)
                throw new InvalidOperationException("Pošiljka je već poslata");

            shipment.TrackingNumber = request.TrackingNumber.Trim();
            shipment.ShippedAt = DateTime.UtcNow;
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);

            if (auction.CurrentWinnerId.HasValue)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = auction.CurrentWinnerId.Value,
                    Type = NotificationType.ItemShipped,
                    Message = $"Prodavac je poslao „{item.Title}“. Broj pošiljke: {shipment.TrackingNumber}."
                }, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
