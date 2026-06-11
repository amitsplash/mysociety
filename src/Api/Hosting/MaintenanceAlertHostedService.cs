using MySociety.Application.MaintenanceAlerts;

namespace MySociety.Api.Hosting;

public class MaintenanceAlertHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaintenanceAlertHostedService> _logger;
    private readonly TimeSpan _interval;

    public MaintenanceAlertHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<MaintenanceAlertHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var hours = configuration.GetValue("MaintenanceAlerts:IntervalHours", 24);
        _interval = TimeSpan.FromHours(Math.Max(1, hours));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var alertService = scope.ServiceProvider.GetRequiredService<IMaintenanceAlertService>();
                await alertService.ProcessDueAlertsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Maintenance alert processing failed");
            }

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
}
