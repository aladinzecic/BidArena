using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Disputes.Commands.OpenDispute;

public class OpenDisputeCommandHandler : IRequestHandler<OpenDisputeCommand, Guid>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDisputeRepository _disputeRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenDisputeCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IPaymentRepository paymentRepository,
        IDisputeRepository disputeRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _paymentRepository = paymentRepository;
        _disputeRepository = disputeRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(OpenDisputeCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

            var isSeller = item.SellerId == request.UserId;
            var isWinner = auction.CurrentWinnerId == request.UserId;

            if (!isSeller && !isWinner)
                throw new ForbiddenAccessException("Spor mogu otvoriti samo prodavac i pobednik aukcije");

            // Status Disputed ujedno sprečava dva istovremena spora za istu aukciju.
            if (auction.Status != AuctionStatus.Ended)
                throw new InvalidOperationException(
                    $"Spor se može otvoriti samo za aukciju u statusu Ended, trenutni status je {auction.Status}");

            var payment = await _paymentRepository.GetByAuctionIdAsync(auction.Id, cancellationToken);
            if (payment is null || payment.Status != PaymentStatus.Paid)
                throw new InvalidOperationException("Spor se može otvoriti tek nakon što je plaćanje izvršeno");

            var dispute = new Dispute
            {
                Id = Guid.NewGuid(),
                AuctionId = auction.Id,
                OpenedByUserId = request.UserId,
                Reason = request.Reason.Trim(),
                Status = DisputeStatus.Open
            };

            auction.Status = AuctionStatus.Disputed;

            // Isti DbContext prati i aukciju, pa se obe izmene snimaju zajedno.
            await _disputeRepository.AddAsync(dispute, cancellationToken);
            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            var otherParty = isSeller ? auction.CurrentWinnerId : item.SellerId;
            if (otherParty.HasValue)
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = otherParty.Value,
                    Type = NotificationType.DisputeOpened,
                    Message = $"Otvoren je spor za aukciju „{item.Title}“. Administrator će ga razmotriti."
                }, cancellationToken);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return dispute.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
