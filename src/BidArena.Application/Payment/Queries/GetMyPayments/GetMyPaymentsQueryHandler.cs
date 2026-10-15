using AutoMapper;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Application.Payments.Queries.GetMyPayments;

public class GetMyPaymentsQueryHandler : IRequestHandler<GetMyPaymentsQuery, List<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public GetMyPaymentsQueryHandler(IPaymentRepository paymentRepository, IMapper mapper)
    {
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    public async Task<List<PaymentDto>> Handle(GetMyPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetByWinnerIdAsync(request.UserId, cancellationToken);

        return _mapper.Map<List<PaymentDto>>(payments);
    }
}