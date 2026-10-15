using BidArena.Application.Disputes.Dtos;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Disputes.Queries.GetDisputes;

public record GetDisputesQuery(DisputeStatus? Status) : IRequest<List<DisputeDto>>;
