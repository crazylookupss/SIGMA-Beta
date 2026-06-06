using SIGMA.Application.Governance;

namespace SIGMA.Api.Services;

internal sealed class GovernanceBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GovernanceBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public GovernanceBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<GovernanceBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var minutes = config.GetValue("Performance:BackgroundGovernanceIntervalMinutes", 5);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[Governance Background] Service started. Interval: {Interval} minutes",
            _interval.TotalMinutes);

        // Run first scan immediately on startup
        await RunScanAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await RunScanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Governance Background] Scan failed. Will retry after {Interval} minutes",
                    _interval.TotalMinutes);
                // Wait before retrying to avoid tight error loops
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("[Governance Background] Service stopped.");
    }

    private async Task RunScanAsync(CancellationToken ct)
    {
        _logger.LogInformation("[Governance Background] Starting governance scan...");

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGovernanceFindingService>();

        var result = await service.GetFindingsAsync(forceRefresh: true, ct);

        _logger.LogInformation(
            "[Governance Background] Scan completed. Findings: {Total} (Critical={Critical}, Warning={Warning}, Info={Info})",
            result.Summary.Total,
            result.Summary.Critical,
            result.Summary.Warning,
            result.Summary.Info);
    }
}
