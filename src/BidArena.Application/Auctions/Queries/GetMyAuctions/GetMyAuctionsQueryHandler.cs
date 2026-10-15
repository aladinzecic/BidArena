using AutoMapper;
using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetMyAuctions;

public class GetMyAuctionsQueryHandler : IRequestHandler<GetMyAuctionsQuery, List<AuctionListItemDto>>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IMapper _mapper;

    public GetMyAuctionsQueryHandler(IAuctionRepository auctionRepository, IMapper mapper)
    {
        _auctionRepository = auctionRepository;
        _mapper = mapper;
    }

    public async Task<List<AuctionListItemDto>> Handle(GetMyAuctionsQuery request, CancellationToken cancellationToken)
    {
        var items = await _auctionRepository.GetBySellerAsync(request.SellerId, cancellationToken);
        return _mapper.Map<List<AuctionListItemDto>>(items);
    }
}
