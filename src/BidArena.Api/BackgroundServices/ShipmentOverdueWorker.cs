using BidArena.Application.Common.Interfaces;
using BidArena.Application.Shipments.Commands.MarkShipmentOverdue;
using MediatR;

namespace BidArena.Api.BackgroundServices;

public class ShipmentOverdueWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShipmentOverdueWorker> _logger;

    public ShipmentOverdueWorker(IServiceScopeFactory scopeFactory, ILogger<ShipmentOverdueWorker> logger)
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
            List<Guid> ids;
            using (var scope = _scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IShipmentRepository>();
                ids = await repository.GetOverdueUnshippedAuctionIdsAsync(DateTime.UtcNow, cancellationToken);
            }

            foreach (var id in ids)
            {
                try
                {
                    // Svaki zapis u svom scope-u, da greška na jednom ne ruši ostale.
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new MarkShipmentOverdueCommand(id), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Otkazivanje zbog neposlate pošiljke nije uspelo za {AuctionId}", id);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Greška u ciklusu: ShipmentOverdueWorker");
        }
    }
}
