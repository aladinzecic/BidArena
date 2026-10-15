using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.CancelAuction;

public class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelAuctionCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CancelAuctionCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock sprecava da se istovremeno upise ponuda dok se aukcija otkazuje.
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

            var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
                ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

            if (item.SellerId != request.RequesterId)
                throw new ForbiddenAccessException("Možete otkazati samo svoje aukcije");

            switch (auction.Status)
            {
                case AuctionStatus.Draft:
                case AuctionStatus.Scheduled:
                    break;

                case AuctionStatus.Active when auction.CurrentWinnerId is null:
                    break;

                case AuctionStatus.Active:
                    throw new InvalidOperationException("Aukcija sa postojećim ponudama ne može biti otkazana");

                default:
                    throw new InvalidOperationException(
                        $"Aukcija se ne može otkazati iz statusa {auction.Status}");
            }

            auction.Status = AuctionStatus.Cancelled;
            await _auctionRepository.UpdateAsync(auction, cancellationToken);

            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
