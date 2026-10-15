using AutoMapper;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Dtos;
using MediatR;

namespace BidArena.Application.Disputes.Queries.GetMyDisputes;

public class GetMyDisputesQueryHandler : IRequestHandler<GetMyDisputesQuery, List<DisputeDto>>
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IMapper _mapper;

    public GetMyDisputesQueryHandler(IDisputeRepository disputeRepository, IMapper mapper)
    {
        _disputeRepository = disputeRepository;
        _mapper = mapper;
    }

    public async Task<List<DisputeDto>> Handle(GetMyDisputesQuery request, CancellationToken cancellationToken)
    {
        var disputes = await _disputeRepository.GetForUserAsync(request.UserId, cancellationToken);
        return _mapper.Map<List<DisputeDto>>(disputes);
    }
}
