using AutoMapper;
using BidArena.Application.Disputes.Dtos;
using BidArena.Domain.Entities;

namespace BidArena.Application.Disputes.Mappings;

public class DisputeMappingProfile : Profile
{
    public DisputeMappingProfile()
    {
        // DisputeDto je record, pa se Status (enum -> string) podešava kao parametar konstruktora.
        CreateMap<DisputePhoto, DisputePhotoDto>();

        CreateMap<Dispute, DisputeDto>()
            .ForCtorParam(nameof(DisputeDto.Status), o => o.MapFrom(s => s.Status.ToString()))
            .ForCtorParam(nameof(DisputeDto.Photos), o => o.MapFrom(s => s.Photos.OrderBy(p => p.OrderIndex)));
    }
}
