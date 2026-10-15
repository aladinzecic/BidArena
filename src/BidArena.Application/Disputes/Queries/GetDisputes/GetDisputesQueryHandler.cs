using AutoMapper;
using BidArena.Application.Common.Interfaces;
using BidArena.Application.Disputes.Dtos;
using MediatR;

namespace BidArena.Application.Disputes.Queries.GetDisputes;

public class GetDisputesQueryHandler : IRequestHandler<GetDisputesQuery, List<DisputeDto>>
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IMapper _mapper;

    public GetDisputesQueryHandler(IDisputeRepository disputeRepository, IMapper mapper)
    {
        _disputeRepository = disputeRepository;
        _mapper = mapper;
    }

    public async Task<List<DisputeDto>> Handle(GetDisputesQuery request, CancellationToken cancellationToken)
    {
        var disputes = await _disputeRepository.GetByStatusAsync(request.Status, cancellationToken);
        return _mapper.Map<List<DisputeDto>>(disputes);
    }
}
