using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class DataSource
{
    public int DataSourceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DataSourceType SourceType { get; init; } = DataSourceType.OTHER;
    public string? ConnectionDetails { get; init; } // TODO: Store as JSON string; map to complex type later if needed
    public DataSourceStatus Status { get; init; }
    public bool IsDefault { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Key to Project
    public int ProjectId { get; init; }

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;
    public virtual ICollection<Asset> Assets { get; init; } = [];
}
