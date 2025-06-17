using server.Services.Interfaces;
using server.Models.Domain.Enums;

namespace server.Data;

public class StartupValidator
{
    public static async Task ValidateStorageServiceAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        
        var servicesProvider = scope.ServiceProvider;

        var logger = servicesProvider.GetRequiredService<ILogger<StartupValidator>>();
        var storageFactory = servicesProvider.GetRequiredService<IStorageServiceFactory>();
        var sourceType = DataSourceType.MINIO_BUCKET; // TODO: Make this configurable

        logger.LogInformation("Performing startup validation for storage service of type '{SourceType}'...", sourceType);

        try
        {
            var storageService = storageFactory.GetService(sourceType);
            await storageService.ListBucketsAsync();
            logger.LogInformation("Storage service validation successful. Connection established.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Storage service validation failed. The application cannot start without a valid storage connection.");
            throw new InvalidOperationException("Storage service is not available.", ex);
        }
    }
}