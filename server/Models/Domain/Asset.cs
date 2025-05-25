using server.Models.Domain.Enums;

namespace server.Models.Domain;

public record class Asset
{
    public int AssetId { get; init; }
    public string ExternalId { get; init; } = string.Empty;
    public string? Filename { get; init; }
    public string? MimeType { get; init; }
    public long? SizeBytes { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? DurationMs { get; init; }
    public string? Metadata { get; init; }
    public AssetStatus Status { get; init; }
    public DateTime? DeletedAt { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Keys
    public int ProjectId { get; init; }
    public int? DataSourceId { get; init; }

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;
    public virtual DataSource? DataSource { get; init; }

    public virtual ICollection<Task> Tasks { get; init; } = [];
    public virtual ICollection<Annotation> Annotations { get; init; } = [];
    public virtual ICollection<Issue> Issues { get; init; } = [];
}
