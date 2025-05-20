using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

using server;
using server.Data;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

        builder.Services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString)
        );

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
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
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        var jwtSecret = builder.Configuration["JWT:Secret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            // TODO: Properly handle the error - log
            Console.Error.WriteLine("CRITICAL ERROR: JWT:Secret is not configured in appsettings.json or environment variables.");
            Console.Error.WriteLine("Application cannot start without a valid JWT secret for token signing.");

            throw new InvalidOperationException("JWT:Secret is not configured. Application cannot start.");
        }

        // JWT Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.Authority = configuration["JWT:Authority"];
            options.Audience = configuration["JWT:Audience"];
            options.RequireHttpsMetadata = builder.Environment.IsProduction();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = configuration.GetSection("JWT:ValidAudience").Get<string>(),
                ValidIssuer = configuration.GetSection("JWT:ValidIssuer").Get<string>(),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
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

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin", corsPolicyBuilder =>
            {
                corsPolicyBuilder.WithOrigins(configuration["WebApp:ClientUrl"] ?? "http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        var app = builder.Build();

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

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
