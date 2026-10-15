using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.ActivateAuction;

public class ActivateAuctionCommandHandler : IRequestHandler<ActivateAuctionCommand>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;

    public ActivateAuctionCommandHandler(IAuctionRepository auctionRepository, IItemRepository itemRepository)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
    }

    public async Task Handle(ActivateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _auctionRepository.GetByIdAsync(request.AuctionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        var item = await _itemRepository.GetByIdAsync(auction.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {auction.ItemId} ne postoji");

        if (item.SellerId != request.RequesterId)
            throw new ForbiddenAccessException("Možete aktivirati samo svoje aukcije");

        if (auction.Status != AuctionStatus.Draft)
            throw new InvalidOperationException(
                $"Aukcija se može aktivirati samo iz statusa Draft, trenutni status je {auction.Status}");

        if (auction.EndTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Aukcija ne može biti aktivirana jer je vreme kraja već prošlo");

        // Ako je pocetak u buducnosti, aukcija se zakazuje; AuctionStartingWorker je kasnije pokrece.
        auction.Status = auction.StartTime > DateTime.UtcNow
            ? AuctionStatus.Scheduled
            : AuctionStatus.Active;

        await _auctionRepository.UpdateAsync(auction, cancellationToken);
    }
}