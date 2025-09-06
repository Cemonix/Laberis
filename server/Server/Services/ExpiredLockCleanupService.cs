using server.Services.Interfaces;

namespace server.Services;

public class ExpiredLockCleanupService : BackgroundService
{
    private readonly ILogger<ExpiredLockCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public ExpiredLockCleanupService(ILogger<ExpiredLockCleanupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired Lock Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Expired Lock Cleanup Service is running.");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var taskLockingService = scope.ServiceProvider.GetRequiredService<ITaskLockingService>();
                var cleanedLocksCount = await taskLockingService.CleanupExpiredLocksAsync();
                _logger.LogInformation("Cleaned up {Count} expired task locks.", cleanedLocksCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while cleaning up expired locks.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Expired Lock Cleanup Service is stopping.");
    }
}
