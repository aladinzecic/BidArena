using BidArena.Application.Auctions.Commands.StartAuction;
using BidArena.Application.Common.Interfaces;
using MediatR;

namespace BidArena.Api.BackgroundServices;

public class AuctionStartingWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionStartingWorker> _logger;

    public AuctionStartingWorker(IServiceScopeFactory scopeFactory, ILogger<AuctionStartingWorker> logger)
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
            List<Guid> dueIds;

            using (var scope = _scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                dueIds = await repository.GetDueScheduledIdsAsync(DateTime.UtcNow, cancellationToken);
            }

            foreach (var auctionId in dueIds)
            {
                try
                {
                    // Svaka aukcija u svom scope-u, da greška na jednoj ne ruši ostale.
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new StartAuctionCommand(auctionId), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Pokretanje zakazane aukcije {AuctionId} nije uspelo", auctionId);
                }
            }

            if (dueIds.Count > 0)
                _logger.LogInformation("Pokrenuto zakazanih aukcija: {Count}", dueIds.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Greška u ciklusu pokretanja zakazanih aukcija");
        }
    }
}
