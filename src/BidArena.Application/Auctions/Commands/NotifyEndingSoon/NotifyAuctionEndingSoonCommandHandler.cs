using BidArena.Application.Common.Interfaces;
using BidArena.Domain.Entities;
using MediatR;

namespace BidArena.Application.Auctions.Commands.NotifyEndingSoon;

// Obaveštava korisnike koji prate aukciju i trenutnog lidera da aukcija uskoro ističe.
// Jedno obaveštenje po korisniku i aukciji (provera preko Notification.AuctionId).
public class NotifyAuctionEndingSoonCommandHandler : IRequestHandler<NotifyAuctionEndingSoonCommand>
{
    public const int WindowMinutes = 60;

    private readonly IAuctionRepository _auctionRepository;
    private readonly IWatchlistRepository _watchlistRepository;
    private readonly INotificationRepository _notificationRepository;

    public NotifyAuctionEndingSoonCommandHandler(
        IAuctionRepository auctionRepository,
        IWatchlistRepository watchlistRepository,
        INotificationRepository notificationRepository)
    {
        _auctionRepository = auctionRepository;
        _watchlistRepository = watchlistRepository;
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(NotifyAuctionEndingSoonCommand request, CancellationToken cancellationToken)
    {
        var data = await _auctionRepository.GetWithItemAsync(request.AuctionId, cancellationToken);
        if (data is null || data.Auction.Status != AuctionStatus.Active)
            return;

        var minutesLeft = (int)Math.Ceiling((data.Auction.EndTime - DateTime.UtcNow).TotalMinutes);
        if (minutesLeft <= 0)
            return;

        var recipients = await _watchlistRepository.GetWatcherIdsAsync(request.AuctionId, cancellationToken);
        if (data.Auction.CurrentWinnerId.HasValue && !recipients.Contains(data.Auction.CurrentWinnerId.Value))
            recipients.Add(data.Auction.CurrentWinnerId.Value);

        foreach (var userId in recipients)
        {
            if (await _notificationRepository.ExistsAsync(
                    userId, NotificationType.AuctionEndingSoon, request.AuctionId, cancellationToken))
                continue;

            var message = $"Aukcija „{data.Item.Title}“ se završava za oko {minutesLeft} min. " +
                          $"Trenutna cena: {data.Auction.CurrentPrice:N2}.";

            await _notificationRepository.AddAsync(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AuctionId = request.AuctionId,
                Type = NotificationType.AuctionEndingSoon,
                Message = message
            }, cancellationToken);
        }
    }
}
