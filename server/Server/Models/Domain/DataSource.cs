using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class DataSource
{
    public int DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DataSourceType SourceType { get; set; } = DataSourceType.OTHER;
    public string? ConnectionDetails { get; set; } // TODO: Store as JSON string; map to complex type later if needed
    public DataSourceStatus Status { get; set; }
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Key to Project
    public int ProjectId { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<Asset> Assets { get; set; } = [];
}
