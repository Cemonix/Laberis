using server.Models.Domain.Enums;
using server.Models.DTOs.Configuration;
using server.Services.Interfaces;
using System.Text.Json;

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
        private PermissionConfiguration? _configuration;
        private readonly Lock _lock = new();

        public PermissionConfigurationService(
            ILogger<PermissionConfigurationService> logger,
            IWebHostEnvironment environment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public PermissionConfigurationDto GetPermissionConfiguration()
        {
            EnsureConfigurationLoaded();

            if (_configuration == null)
            {
                throw new InvalidOperationException("Permission configuration could not be loaded");
            }

            return new PermissionConfigurationDto
            {
                Permissions = _configuration.Permissions,
                RolePermissions = _configuration.RolePermissions
            };
        }

        public bool HasPermission(ProjectRole role, string permission)
        {
            if (_configuration == null)
            {
                _logger.LogWarning("Permission configuration not loaded, denying permission check for role {Role}, permission {Permission}", role, permission);
                return false;
            }

            var roleKey = role.ToString();
            if (!_configuration.RolePermissions.TryGetValue(roleKey, out var permissions))
            {
                _logger.LogWarning("Role {Role} not found in permission configuration", role);
                return false;
            }

            return permissions.Contains(permission);
        }

        public HashSet<string> GetPermissionsForRole(ProjectRole role)
        {
            if (_configuration == null)
            {
                _logger.LogWarning("Permission configuration not loaded, returning empty permissions for role {Role}", role);
                return [];
            }

            var roleKey = role.ToString();
            if (_configuration.RolePermissions.TryGetValue(roleKey, out var permissions))
            {
                return [.. permissions];
            }

            _logger.LogWarning("Role {Role} not found in permission configuration", role);
            return [];
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
                    var configPath = Path.Combine(_environment.ContentRootPath, "Configuration", "project-permissions.json");

                    if (!File.Exists(configPath))
                    {
                        throw new FileNotFoundException($"Permission configuration file not found at {configPath}");
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