using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace server.Services.Storage;

public class StorageServiceFactory : IStorageServiceFactory
{
    private readonly IDictionary<DataSourceType, IStorageService> _storageServices;

    public StorageServiceFactory(IEnumerable<IStorageService> storageServices)
    {
        // This dictionary maps each DataSourceType to its corresponding service implementation.
        _storageServices = storageServices.ToDictionary(s => s.ForType);
    }

    public IStorageService GetService(DataSourceType sourceType)
    {
        if (_storageServices.TryGetValue(sourceType, out var service))
        {
            return service;
        }

        throw new NotSupportedException($"No storage service registered for source type: {sourceType}");
    }
}