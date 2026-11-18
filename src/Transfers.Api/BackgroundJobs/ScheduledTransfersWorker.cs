using Microsoft.Extensions.Options;
using Transfers.Application.Transfers;

namespace Transfers.Api.BackgroundJobs
{
    public sealed class ScheduledTransfersWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SchedulerOptions> options,
        ILogger<ScheduledTransfersWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = options.Value;
            using var timer = new PeriodicTimer(settings.Interval);

            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var transferService = scope.ServiceProvider.GetRequiredService<TransferService>();

                    var result = await transferService.ProcessDueScheduledAsync(settings.BatchSize, stoppingToken);

                    if (result.Processed > 0 || result.Skipped > 0)
                    {
                        logger.LogInformation(
                            "Scheduled transfers: {Processed} processed, {Skipped} skipped",
                            result.Processed,
                            result.Skipped);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Scheduled transfers cycle failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
