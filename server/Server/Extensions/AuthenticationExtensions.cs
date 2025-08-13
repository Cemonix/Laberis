using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using server.Authentication;
using server.Configs;

namespace server.Extensions;

/// <summary>
/// Extension methods for configuring authentication services.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT authentication and fake authentication for development.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        if (jwtSettings == null)
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("JWT settings are not configured properly.");
            Environment.Exit(1);
        }

        // JWT Authentication
        var useFakeUser = configuration.GetValue<bool>("Authentication:UseFakeUser");
        if (useFakeUser && !environment.IsDevelopment())
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("Fake authentication is only allowed in Development environment.");
            Environment.Exit(1);
        }

        if (useFakeUser && environment.IsDevelopment())
        {
            Console.WriteLine("--> FAKE AUTHENTICATION IS ENABLED. All requests will be authenticated as the fake user.");

            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "FakeScheme";
                options.DefaultChallengeScheme = "FakeScheme";
            });

            ConfigureJwtBearer(authBuilder, jwtSettings, environment);
            authBuilder.AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("FakeScheme", options => { });
        }
        else
        {
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            ConfigureJwtBearer(authBuilder, jwtSettings, environment);
        }

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication with appropriate settings for the environment.
    /// </summary>
    /// <param name="authBuilder">The authentication builder.</param>
    /// <param name="jwtSettings">The JWT settings.</param>
    /// <param name="environment">The hosting environment.</param>
    private static void ConfigureJwtBearer(AuthenticationBuilder authBuilder, JwtSettings jwtSettings, IWebHostEnvironment environment)
    {
        authBuilder.AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Authority = jwtSettings.Authority;
            options.Audience = jwtSettings.ValidAudience;
            options.RequireHttpsMetadata = environment.IsProduction();
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