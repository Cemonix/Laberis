using System;
using server.Models.Domain.Enums;

namespace server.Services.Interfaces;

public interface IStorageServiceFactory
{
    /// <summary>
    /// Retrieves the appropriate storage service based on the specified data source type.
    /// </summary>
    /// <param name="sourceType">The type of the data source.</param>
    /// <returns>An instance of IStorageService that handles the given source type.</returns>
    /// <exception cref="NotSupportedException">Thrown when no service is registered for the specified source type.</exception>
    IStorageService GetService(DataSourceType sourceType);
}
