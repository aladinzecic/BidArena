using AutoMapper;
using BidArena.Application.Auctions.Dtos;
using BidArena.Application.Auctions.Models;
using BidArena.Domain;
using BidArena.Domain.Entities;

namespace BidArena.Application.Auctions.Mappings;

public class AuctionMappingProfile : Profile
{
    public AuctionMappingProfile()
    {
        CreateMap<ItemPhoto, ItemPhotoDto>();

        CreateMap<AuctionWithItem, AuctionListItemDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Auction.Id))
            .ForMember(d => d.ItemId, o => o.MapFrom(s => s.Item.Id))
            .ForMember(d => d.Title, o => o.MapFrom(s => s.Item.Title))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Item.Category.ToString()))
            .ForMember(d => d.Condition, o => o.MapFrom(s => s.Item.Condition.ToString()))
            .ForMember(d => d.Location, o => o.MapFrom(s => s.Item.Location))
            .ForMember(d => d.MainPhotoUrl, o => o.MapFrom(s => s.MainPhotoUrl))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Auction.Type.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Auction.Status.ToString()))
            .ForMember(d => d.CurrentPrice, o => o.MapFrom(s =>
                s.Auction.Type == AuctionType.Dutch && s.Auction.Status == AuctionStatus.Active && s.Auction.CurrentWinnerId == null
                    ? DutchPricing.CurrentPrice(s.Auction, DateTime.UtcNow)
                    : s.Auction.CurrentPrice))
            .ForMember(d => d.BidIncrement, o => o.MapFrom(s => s.Auction.BidIncrement))
            .ForMember(d => d.BidCount, o => o.MapFrom(s => s.BidCount))
            .ForMember(d => d.StartTime, o => o.MapFrom(s => s.Auction.StartTime))
            .ForMember(d => d.EndTime, o => o.MapFrom(s => s.Auction.EndTime));

        CreateMap<AuctionWithItem, AuctionDetailsDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Auction.Id))
            .ForMember(d => d.SellerId, o => o.MapFrom(s => s.Item.SellerId))
            .ForMember(d => d.ItemId, o => o.MapFrom(s => s.Item.Id))
            .ForMember(d => d.Title, o => o.MapFrom(s => s.Item.Title))
            .ForMember(d => d.DescriptionMarkdown, o => o.MapFrom(s => s.Item.DescriptionMarkdown))
            .ForMember(d => d.Category, o => o.MapFrom(s => s.Item.Category.ToString()))
            .ForMember(d => d.Condition, o => o.MapFrom(s => s.Item.Condition.ToString()))
            .ForMember(d => d.Location, o => o.MapFrom(s => s.Item.Location))
            .ForMember(d => d.Photos, o => o.MapFrom(s => s.Item.Photos.OrderBy(p => p.OrderIndex)))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Auction.Type.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Auction.Status.ToString()))
            .ForMember(d => d.StartingPrice, o => o.MapFrom(s => s.Auction.StartingPrice))
            .ForMember(d => d.CurrentPrice, o => o.MapFrom(s =>
                s.Auction.Type == AuctionType.Dutch && s.Auction.Status == AuctionStatus.Active && s.Auction.CurrentWinnerId == null
                    ? DutchPricing.CurrentPrice(s.Auction, DateTime.UtcNow)
                    : s.Auction.CurrentPrice))
            .ForMember(d => d.BidIncrement, o => o.MapFrom(s => s.Auction.BidIncrement))
            .ForMember(d => d.MinimumNextBid, o => o.MapFrom(s =>
                s.Auction.Type == AuctionType.Dutch
                    ? DutchPricing.CurrentPrice(s.Auction, DateTime.UtcNow)
                    : s.Auction.CurrentWinnerId == null
                        ? s.Auction.StartingPrice
                        : s.Auction.CurrentPrice + s.Auction.BidIncrement))
            .ForMember(d => d.CurrentWinnerId, o => o.MapFrom(s => s.Auction.CurrentWinnerId))
            .ForMember(d => d.HasReservePrice, o => o.MapFrom(s => s.Auction.Type != AuctionType.Dutch && s.Auction.ReservePrice != null))
            .ForMember(d => d.ReserveMet, o => o.MapFrom(s =>
                s.Auction.ReservePrice == null ||
                (s.Auction.CurrentWinnerId != null && s.Auction.CurrentPrice >= s.Auction.ReservePrice)))
            .ForMember(d => d.BidCount, o => o.MapFrom(s => s.BidCount))
            .ForMember(d => d.StartTime, o => o.MapFrom(s => s.Auction.StartTime))
            .ForMember(d => d.EndTime, o => o.MapFrom(s => s.Auction.EndTime))
            .ForMember(d => d.AntiSnipeWindowMinutes, o => o.MapFrom(s => s.Auction.AntiSnipeWindowMinutes))
            .ForMember(d => d.AntiSnipeExtensionMinutes, o => o.MapFrom(s => s.Auction.AntiSnipeExtensionMinutes));
    }
}