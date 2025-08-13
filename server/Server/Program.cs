using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Minio;
using server.Data;
using server.Configs;
using server.Extensions;
using server.Models.Internal;
using server.Services.EventHandlers;
using System.Text.Json;

namespace server;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

#if DEBUG
        try
        {
            DotNetEnv.Env.TraversePath().Load();
        }
        catch (Exception ex)
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("Failed to load .env file. Ensure the file exists and is correctly formatted.");
            appStartupErrorWriter.WriteLine(ex.Message);
            Environment.Exit(1);
        }
#endif

        // Configure JSON file sources
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        builder.Configuration
            .AddEnvironmentVariables()
            .AddJsonFile($"{baseDirectory}/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"{baseDirectory}/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        var configuration = builder.Configuration;

        // Add application configurations with validation
        builder.Services.AddApplicationConfigurations(configuration);

        // Get validated settings for further configuration
        var webAppSettings = configuration.GetSection(WebAppSettings.SectionName).Get<WebAppSettings>()
            ?? throw new InvalidOperationException("WebApp settings are not configured properly.");

        // Configure database and Identity
        builder.Services.AddDatabaseServices(configuration);

        // Configure authentication
        builder.Services.AddApplicationAuthentication(configuration, builder.Environment);

        // Configure authorization policies
        builder.Services.AddApplicationAuthorization();

        // Configure CORS
        builder.Services.AddApplicationCors(webAppSettings);

        // Configure rate limiting
        builder.Services.AddApplicationRateLimiting(configuration);

        // Register repositories
        builder.Services.AddRepositories();

        // Register storage services
        builder.Services.AddStorageServices(configuration);

        // Register domain event services
        builder.Services.AddDomainEventServices();

        // Register business services  
        builder.Services.AddBusinessServices();

        // Add framework services
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Laberis API",
                Description = "API for managing and handling data for machine learning and image processing.",
            });
            var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddScoped<DataSeeder>();

        // Build the application
        var app = builder.Build();

        // Register domain event handlers after the app is built
        app.RegisterDomainEventHandlers();

        // Configure database
        await app.ConfigureDatabaseAsync();

        // Configure middleware pipeline
        app.ConfigureApplicationPipeline();

        app.Run();
    }
}