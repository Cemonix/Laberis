using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Globalization;
using server.Configs;

namespace server.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting services.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Configures rate limiting with different policies for different endpoint types.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSettings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>();
        if (rateLimitSettings == null)
        {
            TextWriter appStartupErrorWriter = Console.Error;
            appStartupErrorWriter.WriteLine("Rate limiting settings are not configured properly.");
            Environment.Exit(1);
        }

        services.AddRateLimiter(options =>
        {
            // Global limiter applies to all endpoints by default
            // Partition by user identity for authenticated users, by IP for anonymous users
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? 
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = rateLimitSettings.GlobalPermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitSettings.GlobalWindowInSeconds),
                        QueueLimit = rateLimitSettings.GlobalQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));

            // Authentication endpoints - more restrictive
            options.AddFixedWindowLimiter("auth", options =>
            {
                options.PermitLimit = rateLimitSettings.AuthPermitLimit;
                options.Window = TimeSpan.FromSeconds(rateLimitSettings.AuthWindowInSeconds);
                options.QueueLimit = rateLimitSettings.AuthQueueLimit;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.AutoReplenishment = true;
            });

            // File upload endpoints - moderate restrictions
            options.AddFixedWindowLimiter("upload", options =>
            {
                options.PermitLimit = rateLimitSettings.UploadPermitLimit;
                options.Window = TimeSpan.FromSeconds(rateLimitSettings.UploadWindowInSeconds);
                options.QueueLimit = rateLimitSettings.UploadQueueLimit;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.AutoReplenishment = true;
            });

            // Project management endpoints - moderate restrictions
            options.AddFixedWindowLimiter("project", options =>
            {
                options.PermitLimit = rateLimitSettings.ProjectPermitLimit;
                options.Window = TimeSpan.FromSeconds(rateLimitSettings.ProjectWindowInSeconds);
                options.QueueLimit = rateLimitSettings.ProjectQueueLimit;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.AutoReplenishment = true;
            });

            // Public/read-only endpoints - less restrictive
            options.AddFixedWindowLimiter("public", options =>
            {
                options.PermitLimit = rateLimitSettings.PublicPermitLimit;
                options.Window = TimeSpan.FromSeconds(rateLimitSettings.PublicWindowInSeconds);
                options.QueueLimit = rateLimitSettings.PublicQueueLimit;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.AutoReplenishment = true;
            });

            // Custom rejection handler
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                // Add Retry-After header if available
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                }

                // Log rate limit exceeded
                var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
                var userIdentifier = context.HttpContext.User.Identity?.Name ?? 
                    context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                logger?.LogWarning("Rate limit exceeded for user/IP: {UserIdentifier} on endpoint: {Endpoint}", 
                    userIdentifier, context.HttpContext.Request.Path);

                // Return JSON response
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfterSeconds = retryAfter != TimeSpan.Zero ? (int)retryAfter.TotalSeconds : (int?)null
                }, cancellationToken);
            };
        });

        return services;
    }
}