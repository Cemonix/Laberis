namespace server.Models.DTOs.Dashboard;

/// <summary>
/// DTO for dashboard configuration data transfer
/// </summary>
public class DashboardConfigurationDto
{
    public int DashboardConfigurationId { get; set; }
    public string ConfigurationData { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
}

/// <summary>
/// DTO for creating/updating dashboard configuration
/// </summary>
public class CreateUpdateDashboardConfigurationDto
{
    public string ConfigurationData { get; set; } = "{}";
}