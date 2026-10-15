using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Payments.Commands.MarkPaymentOverdue;

public class MarkPaymentOverdueCommandHandler : IRequestHandler<MarkPaymentOverdueCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkPaymentOverdueCommandHandler(
        IPaymentRepository paymentRepository,
        INotificationRepository notificationRepository,
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _notificationRepository = notificationRepository;
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(MarkPaymentOverdueCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var payment = await _paymentRepository.GetByIdForUpdateAsync(request.PaymentId, cancellationToken);
            var now = DateTime.UtcNow;

            // Ponovna provera pod lock-om: pobednik je možda platio u međuvremenu.
            if (payment is null || payment.Status != PaymentStatus.Pending || payment.DueDate > now)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            payment.Status = PaymentStatus.Overdue;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = payment.WinnerId,
                Type = NotificationType.PaymentDue,
                Message = $"Rok za plaćanje iznosa {payment.Amount:N2} je istekao."
            }, cancellationToken);

            // Posledice: aukcija se otkazuje, kupac dobija kazneni poen, prodavac se obaveštava.
            var auction = await _auctionRepository.GetByIdForUpdateAsync(payment.AuctionId, cancellationToken);
            if (auction is not null && auction.Status == AuctionStatus.Ended)
            {
                auction.Status = AuctionStatus.Cancelled;
                await _auctionRepository.UpdateAsync(auction, cancellationToken);

                await _userRepository.IncrementCancelledAuctionsAsync(payment.WinnerId, cancellationToken);

                var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken);
                if (item is not null)
                {
                    await _notificationRepository.AddAsync(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = item.SellerId,
                        Type = NotificationType.AuctionCancelled,
                        Message = $"Kupac nije platio „{item.Title}“ u roku, pa je aukcija otkazana. " +
                                  "Predmet možete ponovo staviti na aukciju."
                    }, cancellationToken);
                }
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
