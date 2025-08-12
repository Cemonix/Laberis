using server.Models.Domain.Enums;
using server.Models.DTOs.Configuration;
using server.Services.Interfaces;
using System.Text.Json;
using server.Data;
using Microsoft.EntityFrameworkCore;

namespace server.Services
{
    /// <summary>
    /// Service for managing project permission configuration.
    /// Loads permission rules from configuration file and provides them to both backend and frontend.
    /// </summary>
    public class PermissionConfigurationService : IPermissionConfigurationService
    {
        private readonly ILogger<PermissionConfigurationService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private PermissionConfiguration? _configuration;
        private readonly Lock _lock = new();

        public PermissionConfigurationService(
            ILogger<PermissionConfigurationService> logger,
            IWebHostEnvironment environment,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }


        public bool HasPermission(ProjectRole role, string permission)
        {
            if (_configuration == null)
            {
                _logger.LogWarning("Permission configuration not loaded, denying permission check for role {Role}, permission {Permission}", role, permission);
                return false;
            }

            var roleKey = role.ToString();
            if (!_configuration.Roles.TryGetValue(roleKey, out var roleDefinition))
            {
                _logger.LogWarning("Role {Role} not found in permission configuration", role);
                return false;
            }

            return roleDefinition.Permissions.Contains(permission);
        }

        public HashSet<string> GetPermissionsForRole(ProjectRole role)
        {
            if (_configuration == null)
            {
                _logger.LogWarning("Permission configuration not loaded, returning empty permissions for role {Role}", role);
                return [];
            }

            var roleKey = role.ToString();
            if (_configuration.Roles.TryGetValue(roleKey, out var roleDefinition))
            {
                return [.. roleDefinition.Permissions];
            }

            _logger.LogWarning("Role {Role} not found in permission configuration", role);
            return [];
        }

        public HashSet<string> GetGlobalPermissions()
        {
            if (_configuration == null)
            {
                _logger.LogWarning("Permission configuration not loaded, returning empty global permissions");
                return [];
            }

            return [.. _configuration.GlobalPermissions];
        }

        public async Task<UserPermissionContext> BuildUserPermissionContextAsync(string userId)
        {
            EnsureConfigurationLoaded();

            if (_configuration == null)
            {
                throw new InvalidOperationException("Permission configuration could not be loaded");
            }

            var context = new UserPermissionContext
            {
                GlobalPermissions = GetGlobalPermissions()
            };

            // Get user's project memberships using a scoped DbContext
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LaberisDbContext>();
            
            var projectMemberships = await dbContext.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .Select(pm => new { pm.ProjectId, pm.Role })
                .ToListAsync();

            // Build project-specific permissions
            foreach (var membership in projectMemberships)
            {
                var rolePermissions = GetPermissionsForRole(membership.Role);
                context.ProjectPermissions[membership.ProjectId] = rolePermissions;
                
                // Add to overall permissions set
                foreach (var permission in rolePermissions)
                {
                    context.Permissions.Add(permission);
                }
            }

            // Add global permissions to overall permissions
            foreach (var globalPermission in context.GlobalPermissions)
            {
                context.Permissions.Add(globalPermission);
            }

            _logger.LogDebug("Built permission context for user {UserId} with {PermissionCount} total permissions across {ProjectCount} projects", 
                userId, context.Permissions.Count, context.ProjectPermissions.Count);

            return context;
        }

        public async Task<HashSet<string>> GetPagePermissionsAsync(string userId, string page, int? projectId = null)
        {
            var context = await BuildUserPermissionContextAsync(userId);
            var permissions = new HashSet<string>(context.GlobalPermissions);

            // Add project-specific permissions if projectId is provided
            if (projectId.HasValue && context.ProjectPermissions.TryGetValue(projectId.Value, out var projectPermissions))
            {
                foreach (var permission in projectPermissions)
                {
                    permissions.Add(permission);
                }
            }

            // TODO: Implement page-specific filtering if needed
            // For now, return all applicable permissions for the user
            _logger.LogDebug("Retrieved {PermissionCount} permissions for user {UserId} on page {Page} with projectId {ProjectId}", 
                permissions.Count, userId, page, projectId);

            return permissions;
        }

        public void ReloadConfiguration()
        {
            lock (_lock)
            {
                _configuration = null;
            }
            EnsureConfigurationLoaded();
        }

        private void EnsureConfigurationLoaded()
        {
            if (_configuration != null)
            {
                return;
            }

            lock (_lock)
            {
                if (_configuration != null)
                {
                    return;
                }

                try
                {
                    var configPath = Path.Combine(_environment.ContentRootPath, "Configs", "project-permissions.json");
                    
                    _logger.LogDebug("Looking for permission configuration at: {ConfigPath}", configPath);
                    _logger.LogDebug("ContentRootPath is: {ContentRootPath}", _environment.ContentRootPath);

                    if (!File.Exists(configPath))
                    {
                        // Try alternative path in case we're running from parent directory
                        var alternativePath = Path.Combine(_environment.ContentRootPath, "Server", "Configs", "project-permissions.json");
                        _logger.LogDebug("Trying alternative path: {AlternativePath}", alternativePath);
                        
                        if (File.Exists(alternativePath))
                        {
                            configPath = alternativePath;
                        }
                        else
                        {
                            throw new FileNotFoundException($"Permission configuration file not found at {configPath} or {alternativePath}");
                        }
                    }

                    var configJson = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<PermissionConfiguration>(configJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? throw new InvalidOperationException("Failed to deserialize permission configuration");
                    _configuration = config;
                    _logger.LogInformation("Permission configuration loaded successfully from {ConfigPath}", configPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load permission configuration");
                    throw;
                }
            }
        }
    }
}