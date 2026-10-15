using AutoMapper;
using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Watchlist.Queries.GetMyWatchlist;

public class GetMyWatchlistQueryHandler : IRequestHandler<GetMyWatchlistQuery, List<AuctionListItemDto>>
{
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly IMapper _mapper;

    public GetMyWatchlistQueryHandler(IWatchlistRepository watchlistRepository, IMapper mapper)
    {
        _watchlistRepository = watchlistRepository;
        _mapper = mapper;
    }

    public async Task<List<AuctionListItemDto>> Handle(
        GetMyWatchlistQuery request, CancellationToken cancellationToken)
    {
        var items = await _watchlistRepository.GetMyWatchlistAsync(request.UserId, cancellationToken);
        return _mapper.Map<List<AuctionListItemDto>>(items);
    }
}
