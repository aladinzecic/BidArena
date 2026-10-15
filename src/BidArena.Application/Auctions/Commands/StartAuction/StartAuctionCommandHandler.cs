using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.StartAuction;

public class StartAuctionCommandHandler : IRequestHandler<StartAuctionCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartAuctionCommandHandler(IAuctionRepository auctionRepository, IUnitOfWork unitOfWork)
    {
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(StartAuctionCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var auction = await _auctionRepository.GetByIdForUpdateAsync(request.AuctionId, cancellationToken);

            // Ponovna provera pod lock-om: prodavac je u medjuvremenu mogao da otkaze aukciju.
            if (auction is null || auction.Status != AuctionStatus.Scheduled || auction.StartTime > DateTime.UtcNow)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return;
            }

            auction.Status = AuctionStatus.Active;
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
