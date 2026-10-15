using AutoMapper;
using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetAuctionById;

public class GetAuctionByIdQueryHandler : IRequestHandler<GetAuctionByIdQuery, AuctionDetailsDto>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IMapper _mapper;

    public GetAuctionByIdQueryHandler(IAuctionRepository auctionRepository, IMapper mapper)
    {
        _auctionRepository = auctionRepository;
        _mapper = mapper;
    }

    public async Task<AuctionDetailsDto> Handle(GetAuctionByIdQuery request, CancellationToken cancellationToken)
    {
        var auctionWithItem = await _auctionRepository.GetWithItemAsync(request.AuctionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aukcija {request.AuctionId} ne postoji");

        return _mapper.Map<AuctionDetailsDto>(auctionWithItem);
    }
}
