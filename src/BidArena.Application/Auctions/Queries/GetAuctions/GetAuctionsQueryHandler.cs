using AutoMapper;
using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Auctions.Models;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Common.Models;
using BidArena.Application.Items;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetAuctions;

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, PagedResult<AuctionListItemDto>>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IMapper _mapper;

    public GetAuctionsQueryHandler(IAuctionRepository auctionRepository, IMapper mapper)
    {
        _auctionRepository = auctionRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<AuctionListItemDto>> Handle(GetAuctionsQuery request, CancellationToken cancellationToken)
    {
        // Bez eksplicitnog statusa prikazujemo aktivne aukcije.
        ItemCategory? category = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            if (!ItemCategories.TryParse(request.Category, out var parsed))
                throw new ArgumentException($"Nepoznata kategorija: {request.Category}");
            category = parsed;
        }

        var criteria = new AuctionSearchCriteria(
            request.Status ?? AuctionStatus.Active,
            category,
            request.Search,
            request.Page,
            request.PageSize);

        var result = await _auctionRepository.SearchAsync(criteria, cancellationToken);

        return new PagedResult<AuctionListItemDto>(
            _mapper.Map<List<AuctionListItemDto>>(result.Items),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}