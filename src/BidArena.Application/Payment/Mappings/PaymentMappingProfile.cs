using AutoMapper;
using BidArena.Application.Payments.Queries.GetMyPayments;
using BidArena.Domain.Entities;

namespace BidArena.Application.Payments.Mappings;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // PaymentDto je record, pa se Status (enum -> string) podešava kao parametar konstruktora.
        CreateMap<Payment, PaymentDto>()
            .ForCtorParam(nameof(PaymentDto.Status), o => o.MapFrom(s => s.Status.ToString()));
    }
}
