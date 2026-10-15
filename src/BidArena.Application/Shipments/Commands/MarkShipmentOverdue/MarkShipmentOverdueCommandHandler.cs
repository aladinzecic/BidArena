using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Shipments.Commands.MarkShipmentOverdue;

// Prodavac nije poslao predmet u roku: aukcija se otkazuje, kupcu se vraća novac (Refunded),
// prodavac dobija kazneni poen (CancelledAuctionsCount), a obe strane se obaveštavaju.
public class MarkShipmentOverdueCommandHandler : IRequestHandler<MarkShipmentOverdueCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkShipmentOverdueCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IShipmentRepository shipmentRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _shipmentRepository = shipmentRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkShipmentOverdueCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken);
            var now = DateTime.UtcNow;

            // Ponovna provera pod lock-om (prodavac je možda poslao ili je otvoren spor).
            if (auction is null || auction.Status != AuctionStatus.Ended)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            var shipment = await _shipmentRepository.GetByAuctionIdForUpdateAsync(request.AuctionId, cancellationToken);
            if (shipment is null || shipment.ShippedAt is not null || shipment.DueDate > now)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            auction.Status = AuctionStatus.Cancelled;
            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            var payment = await _paymentRepository.GetByAuctionIdForUpdateAsync(request.AuctionId, cancellationToken);
            if (payment is not null && payment.Status == PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.Refunded;
                await _paymentRepository.UpdateAsync(payment, cancellationToken);
            }

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken);
            var title = item?.Title ?? "predmet";

            if (item is not null)
            {
                await _userRepository.IncrementCancelledAuctionsAsync(item.SellerId, cancellationToken);

                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = item.SellerId,
                    AuctionId = auction.Id,
                    Type = NotificationType.AuctionCancelled,
                    Message = $"Niste poslali „{title}“ u roku, pa je aukcija otkazana, a kupcu se vraća novac."
                }, cancellationToken);
            }

            if (auction.CurrentWinnerId.HasValue)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = auction.CurrentWinnerId.Value,
                    AuctionId = auction.Id,
                    Type = NotificationType.AuctionCancelled,
                    Message = $"Prodavac nije poslao „{title}“ u roku. Aukcija je otkazana i novac vam se vraća."
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
