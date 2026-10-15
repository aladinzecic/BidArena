using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Common.Models;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Queries.GetAuctions;

public record GetAuctionsQuery(
    AuctionStatus? Status,
    string? Category,
    string? Search,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AuctionListItemDto>>;