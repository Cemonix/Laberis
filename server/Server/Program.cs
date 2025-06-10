using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

using server;
using server.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using server.Configs;
using server.Models;
using server.Repositories;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Services;
using server.Services.Storage;
using Microsoft.AspNetCore.Authentication;
using server.Authentication;
using System.Security.Claims;
using Npgsql;
using server.Models.Domain.Enums;
using Minio;

namespace server;

public class Program
{
    public async static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

#if DEBUG
        string envFilePath = "../.env";
        if (!File.Exists(envFilePath))
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("No .env file found. Ensure environment variables are set correctly.");
            Environment.Exit(1);
        }
        DotNetEnv.Env.Load(envFilePath);
#endif

        builder.Configuration
            .AddEnvironmentVariables()
            .AddJsonFile("Server/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"Server/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        var configuration = builder.Configuration;

        #region Service Configuration

        // Configure and validate Minio settings
        builder.Services.AddOptions<MinioSettings>()
            .Bind(configuration.GetSection(MinioSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate WebApp settings
        builder.Services.AddOptions<WebAppSettings>()
            .Bind(configuration.GetSection(WebAppSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate JWT settings
        builder.Services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure and validate Admin User settings
        builder.Services.AddOptions<AdminUserSettings>()
            .Bind(configuration.GetSection(AdminUserSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

        builder.Services.AddDbContext<LaberisDbContext>(options =>
            options.UseNpgsql(connectionString, NpgsqlEnumMapper.ConfigureEnums)
        );

        builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<LaberisDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddOptions<MinioSettings>()
            .Bind(configuration.GetSection(MinioSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddMinio(options =>
        {
            var minioSettings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>();
            if (minioSettings != null)
            {
                options.WithEndpoint(minioSettings.Endpoint);
                options.WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey);
                options.WithSSL(minioSettings.UseSsl);
            }
        });

        builder.Services.AddScoped<IStorageService, MinioStorageService>();
        builder.Services.AddScoped<IStorageServiceFactory, StorageServiceFactory>();

        builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
        builder.Services.AddScoped<IProjectService, ProjectService>();
        builder.Services.AddScoped<IDataSourceRepository, DataSourceRepository>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings == null)
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("JWT settings are not configured properly.");
            Environment.Exit(1);
        }

        // JWT Authentication
        var useFakeUser = builder.Configuration.GetValue<bool>("Authentication:UseFakeUser");
        if (useFakeUser && !builder.Environment.IsDevelopment())
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("Fake authentication is only allowed in Development environment.");
            Environment.Exit(1);
        }
        else if (useFakeUser)
        {
            Console.WriteLine("--> FAKE AUTHENTICATION IS ENABLED. All requests will be authenticated as the fake user.");

            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "FakeScheme";
                options.DefaultChallengeScheme = "FakeScheme";
            });

            ConfigureJwtBearer(authBuilder, jwtSettings, builder.Environment);

            authBuilder.AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("FakeScheme", options => { });
        }
        else
        {
            var authBuilder = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            ConfigureJwtBearer(authBuilder, jwtSettings, builder.Environment);
        }

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("RequireAuthenticatedUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClaimTypes.NameIdentifier);
            })
            .AddPolicy("RequireAdminRole", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Admin");
            });

        builder.Services.AddControllers();
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

        var webAppSettings = configuration.GetSection(WebAppSettings.SectionName).Get<WebAppSettings>();
        if (webAppSettings == null)
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("WebApp settings are not configured properly.");
            Environment.Exit(1);
        }

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins(webAppSettings.ClientUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        #endregion

        var app = builder.Build();

        // Ensure the database is created and migrations are applied
        await StartupValidator.ValidateStorageServiceAsync(app.Services);
        
        // TODO: Uncomment after solving the issue with dotnet ef migrations
        // using (var scope = app.Services.CreateScope())
        // {
        //     var dbContext = scope.ServiceProvider.GetRequiredService<LaberisDbContext>();
        //     try
        //     {
        //         await dbContext.Database.MigrateAsync();
        //     }
        //     catch (Exception ex)
        //     {
        //         var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        //         logger.LogError(ex, "An error occurred while migrating the database.");
        //         Environment.Exit(1);
        //     }
        // }

        using (var scope = app.Services.CreateScope())
        {
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

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Laberis API V1");
            });
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();

    }
    
    static void ConfigureJwtBearer(AuthenticationBuilder authBuilder, JwtSettings jwtSettings, IWebHostEnvironment env)
    {
        authBuilder.AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Authority = jwtSettings.Authority;
            options.Audience = jwtSettings.ValidAudience;
            options.RequireHttpsMetadata = env.IsProduction();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = jwtSettings.ValidAudience,
                ValidIssuer = jwtSettings.ValidIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsJsonAsync(new
                        {
                            error = "Token expired",
                            message = "The provided token has expired. Please log in again."
                        });
                    }
                    return Task.CompletedTask;
                },
            };
        });
    }
}
