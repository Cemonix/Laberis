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

namespace server;

public class Program
{
    public async static Task Main(string[] args)
    {
        var logger = new LoggerFactory()
            .CreateLogger<Program>();

#if DEBUG
        string envFilePath = "../.env";
        if (!File.Exists(envFilePath))
        {
            logger.LogError("No .env file found. Ensure environment variables are set correctly.");
            Environment.Exit(1);
        }
        DotNetEnv.Env.Load(envFilePath);
#endif

        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

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

        builder.Services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        builder.Services.AddDbContext<LaberisDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings == null)
        {
            logger.LogError("JWT settings are not configured properly.");
            Environment.Exit(1);
        }

        // JWT Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Authority = jwtSettings.Authority;
            options.Audience = jwtSettings.ValidAudience;
            options.RequireHttpsMetadata = builder.Environment.IsProduction();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = jwtSettings.ValidAudience,
                ValidIssuer = jwtSettings.ValidIssuer,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
            };
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
            logger.LogError("WebApp settings are not configured properly.");
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


        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                await DataSeeder.InitializeAsync(services);
            }
            catch (Exception ex)
            {
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
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowSpecificOrigin");

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
