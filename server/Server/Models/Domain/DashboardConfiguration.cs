namespace server.Models.Domain;

/// <summary>
/// Stores user-specific dashboard widget layout and preferences
/// </summary>
public class DashboardConfiguration
{
    public int DashboardConfigurationId { get; set; }
    public string ConfigurationData { get; set; } = "{}"; // JSON serialized widget configuration
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }

    // Navigation Properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Project Project { get; set; } = null!;
}