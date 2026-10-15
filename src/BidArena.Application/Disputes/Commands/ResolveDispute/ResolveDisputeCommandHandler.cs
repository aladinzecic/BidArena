using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Disputes.Commands.ResolveDispute;

public class ResolveDisputeCommandHandler : IRequestHandler<ResolveDisputeCommand>
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ResolveDisputeCommandHandler(
        IDisputeRepository disputeRepository,
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IPaymentRepository paymentRepository,
        IUserRepository userRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _disputeRepository = disputeRepository;
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _paymentRepository = paymentRepository;
        _userRepository = userRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Redosled zaključavanja: spor pa aukcija.
            var dispute = await _disputeRepository.GetByIdForUpdateAsync(request.DisputeId, cancellationToken)
                ?? throw new KeyNotFoundException($"Spor {request.DisputeId} ne postoji");

            if (dispute.Status != DisputeStatus.UnderReview)
                throw new InvalidOperationException(
                    $"Spor se može rešiti samo iz statusa UnderReview, trenutni status je {dispute.Status}");

            var auction = await _auctionRepository.GetByIdForUpdateAsync(dispute.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {dispute.AuctionId} ne postoji");

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

            dispute.Status = DisputeStatus.Resolved;
            var outcomeLabel = request.Outcome == DisputeOutcome.RefundBuyer
                ? "Povraćaj novca kupcu"
                : "U korist prodavca";
            dispute.Resolution = $"[{outcomeLabel}] {request.Resolution.Trim()}";
            dispute.ResolvedByAdminId = request.AdminId;

            if (auction.Status == AuctionStatus.Disputed)
            {
                if (request.Outcome == DisputeOutcome.RefundBuyer)
                {
                    auction.Status = AuctionStatus.Cancelled;

                    var payment = await _paymentRepository.GetByAuctionIdForUpdateAsync(auction.Id, cancellationToken);
                    if (payment is not null && payment.Status == PaymentStatus.Paid)
                    {
                        payment.Status = PaymentStatus.Refunded;
                        await _paymentRepository.UpdateAsync(payment, cancellationToken);
                    }

                    await _userRepository.IncrementCancelledAuctionsAsync(item.SellerId, cancellationToken);
                }
                else
                {
                    auction.Status = AuctionStatus.Settled;

                    var seller = await _userRepository.GetByIdAsync(item.SellerId, cancellationToken);
                    if (seller is not null)
                    {
                        seller.SuccessfulAuctionsCount++;
                        await _userRepository.UpdateAsync(seller, cancellationToken);
                    }
                }
            }

            // Isti DbContext prati i aukciju, pa se obe izmene snimaju zajedno.
            await _disputeRepository.UpdateAsync(dispute, cancellationToken);
            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            var message = $"Spor za aukciju „{item.Title}“ je rešen. Odluka: {dispute.Resolution}";

            var recipients = new List<Guid> { item.SellerId };
            if (auction.CurrentWinnerId.HasValue && auction.CurrentWinnerId.Value != item.SellerId)
                recipients.Add(auction.CurrentWinnerId.Value);

            foreach (var userId in recipients)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Type = NotificationType.DisputeResolved,
                    Message = message
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
