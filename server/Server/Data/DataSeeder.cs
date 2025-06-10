using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using server.Configs;
using server.Models;

namespace server.Data;

public class DataSeeder
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<DataSeeder>>();
        var adminUserSettings = serviceProvider.GetRequiredService<IOptions<AdminUserSettings>>().Value;
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure the database is created.
        var dbContext = serviceProvider.GetRequiredService<LaberisDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        logger.LogInformation("Initializing database with seed data...");

        // Define Admin Role
        string adminRoleName = adminUserSettings.Role;
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRoleName));
            logger.LogInformation("'{AdminRoleName}' role created.", adminRoleName);
        }

        // Define Admin User
        var adminUserToCreate = new IdentityUser
        {
            Id = adminUserSettings.Id,
            UserName = adminUserSettings.Username,
            Email = adminUserSettings.Email,
            EmailConfirmed = true
        };

        // Check if admin user exists
        if (await userManager.FindByNameAsync(adminUserToCreate.UserName) == null)
        {
            string adminPassword = adminUserSettings.Password;

            var createUserResult = await userManager.CreateAsync(adminUserToCreate, adminPassword);
            if (createUserResult.Succeeded)
            {
                logger.LogInformation("Admin user '{AdminUserName}' created successfully.", adminUserToCreate.UserName);
                await userManager.AddToRoleAsync(adminUserToCreate, adminRoleName);
                logger.LogInformation("Admin user '{AdminUserName}' added to '{AdminRoleName}' role.", adminUserToCreate.UserName, adminRoleName);
            }
            else
            {
                foreach (var error in createUserResult.Errors)
                {
                    logger.LogError("Error creating admin user: {ErrorCode} - {ErrorDescription}", error.Code, error.Description);
                }
            }
        }
        else
        {
            logger.LogInformation("Admin user '{AdminUserName}' already exists.", adminUserToCreate.UserName);
        }
        logger.LogInformation("Database initialization complete.");
    }
}
