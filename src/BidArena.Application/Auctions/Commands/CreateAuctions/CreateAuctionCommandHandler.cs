using BidArena.Application.Common.Exceptions;
using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.CreateAuction;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUserRepository _userRepository;

    public CreateAuctionCommandHandler(
        IAuctionRepository auctionRepository,
        IItemRepository itemRepository,
        IUserRepository userRepository)
    {
        _auctionRepository = auctionRepository;
        _itemRepository = itemRepository;
        _userRepository = userRepository;
    }

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var item = await _itemRepository.GetByIdAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Predmet {request.ItemId} ne postoji");

        if (item.SellerId != request.SellerId)
            throw new ForbiddenAccessException("Možete praviti aukcije samo za svoje predmete");

        var seller = await _userRepository.GetByIdAsync(request.SellerId, cancellationToken);
        if (seller is null || !seller.IsVerified)
            throw new ForbiddenAccessException("Aukcije mogu da prave samo verifikovani prodavci");

        if (!Enum.TryParse<AuctionType>(request.Type, true, out var type))
            throw new ArgumentException($"Nepoznat tip aukcije: {request.Type}");

        if (request.EndTime <= request.StartTime)
            throw new ArgumentException("Vreme kraja mora biti posle vremena početka");

        if (type == AuctionType.Dutch)
        {
            if (request.ReservePrice is null || request.ReservePrice <= 0 || request.ReservePrice >= request.StartingPrice)
                throw new ArgumentException(
                    "Za holandsku aukciju je obavezna donja granica cene (ReservePrice) koja je manja od početne cene");
        }

        var auction = new Auction
        {
            Id = Guid.NewGuid(),
            ItemId = request.ItemId,
            Type = type,
            Status = AuctionStatus.Draft,
            StartingPrice = request.StartingPrice,
            ReservePrice = request.ReservePrice,
            BidIncrement = request.BidIncrement,
            CurrentPrice = request.StartingPrice,
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        var createdAuction = await _auctionRepository.AddAsync(auction, cancellationToken);
        return createdAuction.Id;
    }
}