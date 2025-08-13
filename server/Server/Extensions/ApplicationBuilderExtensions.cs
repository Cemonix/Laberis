using server.Configs;
using server.Data;
using server.Models.Internal;
using server.Services.EventHandlers;
using server.Services.Interfaces;

namespace server.Extensions;

/// <summary>
/// Extension methods for configuring the application request pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures CORS policy for the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="webAppSettings">The web app settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, WebAppSettings webAppSettings)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins(webAppSettings.ClientUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the application middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static WebApplication ConfigureApplicationPipeline(this WebApplication app)
    {
        // Add global exception handling middleware early in the pipeline
        app.UseGlobalExceptionHandling();

        // Don't use UseExceptionHandler since we have our custom middleware
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Laberis API V1");
            });
        }
        else
        {
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseCors("AllowSpecificOrigin");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Configures database-related startup tasks and validates storage service.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task ConfigureDatabaseAsync(this WebApplication app)
    {
        // Skip startup validation and DataSeeder during testing to avoid database provider conflicts
        if (!app.Environment.IsEnvironment("Testing"))
        {
            // Validate storage service connection
            await StartupValidator.ValidateStorageServiceAsync(app.Services);

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                await DataSeeder.InitializeAsync(services);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred during database seeding.");
                Environment.Exit(1);
            }
        }
    }

    /// <summary>
    /// Registers domain event handlers after the application is built.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static void RegisterDomainEventHandlers(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var eventService = scope.ServiceProvider.GetRequiredService<IDomainEventService>();
        var assetImportedHandler = scope.ServiceProvider.GetRequiredService<AssetImportedEventHandler>();
        
        eventService.Subscribe(assetImportedHandler);
    }
}