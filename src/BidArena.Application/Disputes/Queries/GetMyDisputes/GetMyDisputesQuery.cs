using BidArena.Application.Disputes.Dtos;
using MediatR;

namespace BidArena.Application.Disputes.Queries.GetMyDisputes;

public record GetMyDisputesQuery(Guid UserId) : IRequest<List<DisputeDto>>;
