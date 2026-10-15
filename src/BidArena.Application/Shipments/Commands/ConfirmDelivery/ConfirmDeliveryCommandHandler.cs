using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Shipments.Commands.ConfirmDelivery;

public class ConfirmDeliveryCommandHandler : IRequestHandler<ConfirmDeliveryCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmDeliveryCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IShipmentRepository shipmentRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _shipmentRepository = shipmentRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ConfirmDeliveryCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

            if (auction.CurrentWinnerId != request.BuyerId)
                throw new ForbiddenAccessException("Isporuku može potvrditi samo pobednik aukcije");

            if (auction.Status != AuctionStatus.Ended)
                throw new InvalidOperationException(
                    $"Isporuka se može potvrditi samo za aukciju u statusu Ended, trenutni status je {auction.Status}");

            var shipment = await _shipmentRepository.GetByAuctionIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException("Pošiljka ne postoji");

            if (shipment.ShippedAt is null)
                throw new InvalidOperationException("Prodavac još nije poslao pošiljku");

            if (shipment.DeliveredAt is not null)
                throw new InvalidOperationException("Isporuka je već potvrđena");

            shipment.DeliveredAt = DateTime.UtcNow;
            auction.Status = AuctionStatus.Settled;

            // Isti DbContext prati i pošiljku i aukciju, pa se obe izmene snimaju zajedno.
            await _shipmentRepository.UpdateAsync(shipment, cancellationToken);
            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

            var seller = await _userRepository.GetByIdAsync(item.SellerId, cancellationToken);
            if (seller is not null)
            {
                seller.SuccessfulAuctionsCount++;
                await _userRepository.UpdateAsync(seller, cancellationToken);
            }

            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = item.SellerId,
                Type = NotificationType.ItemDelivered,
                Message = $"Kupac je potvrdio prijem predmeta „{item.Title}“. Aukcija je uspešno završena."
            }, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
