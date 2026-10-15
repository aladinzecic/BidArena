using BidArena.Application.Auctions.Commands.CloseAuction;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Api.BackgroundServices;

public class AuctionClosingWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionClosingWorker> _logger;

    public AuctionClosingWorker(IServiceScopeFactory scopeFactory, ILogger<AuctionClosingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Gašenje aplikacije.
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            List<Guid> expiredIds;

            using (var scope = _scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                expiredIds = await repository.GetExpiredActiveIdsAsync(DateTime.UtcNow, cancellationToken);
            }

            foreach (var auctionId in expiredIds)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new CloseAuctionCommand(auctionId), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Zatvaranje aukcije {AuctionId} nije uspelo", auctionId);
                }
            }

            if (expiredIds.Count > 0)
                _logger.LogInformation("Obrađeno isteklih aukcija: {Count}", expiredIds.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Greška u ciklusu zatvaranja aukcija");
        }
    }
}