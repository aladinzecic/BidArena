using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Payments.Commands.PayPayment;

public class PayPaymentCommandHandler : IRequestHandler<PayPaymentCommand>
{
    // Rok u kome prodavac mora da pošalje predmet nakon uplate.
    public const int ShipmentDueDays = 5;

    private readonly IPaymentRepository _paymentRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PayPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IAuctionRepository auctionRepository,
        INotificationRepository notificationRepository,
        IShipmentRepository shipmentRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _auctionRepository = auctionRepository;
        _notificationRepository = notificationRepository;
        _shipmentRepository = shipmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PayPaymentCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = await _paymentRepository.GetByIdForUpdateAsync(request.PaymentId, cancellationToken)
                ?? throw new KeyNotFoundException($"Plaćanje {request.PaymentId} ne postoji");

            if (payment.WinnerId != request.UserId)
                throw new ForbiddenAccessException("Možete platiti samo sopstvene obaveze");

            if (payment.Status == PaymentStatus.Paid)
                throw new InvalidOperationException("Plaćanje je već izvršeno");

            var now = DateTime.UtcNow;

            // Pokriva i slučaj kada job još nije stigao da označi Overdue.
            if (payment.Status == PaymentStatus.Overdue || payment.DueDate <= now)
                throw new InvalidOperationException("Rok za plaćanje je istekao");

            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = now;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            // Uplata je preduslov za slanje: pošiljka dobija rok za prodavca.
            await _shipmentRepository.AddAsync(new Shipment
            {
                Id = Guid.NewGuid(),
                AuctionId = payment.AuctionId,
                DueDate = now.AddDays(ShipmentDueDays)
            }, cancellationToken);

            var data = await _auctionRepository.GetWithItemAsync(payment.AuctionId, cancellationToken);
            if (data is not null)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = data.Item.SellerId,
                    Type = NotificationType.PaymentReceived,
                    Message = $"Kupac je platio „{data.Item.Title}“ u iznosu od {payment.Amount:N2}. " +
                              $"Pošiljku je potrebno poslati u roku od {ShipmentDueDays} dana."
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