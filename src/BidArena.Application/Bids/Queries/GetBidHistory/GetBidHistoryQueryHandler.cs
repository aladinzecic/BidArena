using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Bids.Queries.GetBidHistory;

public class GetBidHistoryQueryHandler : IRequestHandler<GetBidHistoryQuery, List<BidDto>>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IBidRepository _bidRepository;

    public GetBidHistoryQueryHandler(IAuctionRepository auctionRepository, IBidRepository bidRepository)
    {
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
    }

    public async Task<List<BidDto>> Handle(GetBidHistoryQuery request, CancellationToken cancellationToken)
    {
        if (!await _auctionRepository.ExistsAsync(request.AuctionId, cancellationToken))
            throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        var bids = await _bidRepository.GetByAuctionIdAsync(request.AuctionId, cancellationToken);

        // ProxyMaxAmount namerno nije u DTO-u: tajni maksimum drugi bidderi ne smeju da vide.
        return bids
            .Select(b => new BidDto(b.Id, b.BidderId, b.Amount, b.IsProxy, b.CreatedAt))
            .ToList();
    }
}