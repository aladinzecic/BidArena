using BidArena.Application.Common.Interfaces;
using BidArena.Application.Payments.Commands.MarkPaymentOverdue;
using MediatR;

namespace BidArena.Api.BackgroundServices;

public class PaymentOverdueWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentOverdueWorker> _logger;

    public PaymentOverdueWorker(IServiceScopeFactory scopeFactory, ILogger<PaymentOverdueWorker> logger)
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
            List<Guid> overdueIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                overdueIds = await repository.GetOverduePendingIdsAsync(DateTime.UtcNow, cancellationToken);
            }

            foreach (var paymentId in overdueIds)
            {
                try
                {
                    // Svako plaćanje u svom scope-u, da greška na jednom ne ruši ostale.
                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new MarkPaymentOverdueCommand(paymentId), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Označavanje plaćanja {PaymentId} kao Overdue nije uspelo", paymentId);
                }
            }

            if (overdueIds.Count > 0)
                _logger.LogInformation("Obrađeno plaćanja kojima je istekao rok: {Count}", overdueIds.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Greška u ciklusu provere rokova plaćanja");
        }
    }
}