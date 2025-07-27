using server.Models.Domain.Enums;

namespace server.Models.Domain;

public class Asset
{
    public int AssetId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }
    public string? Metadata { get; set; }
    public AssetStatus Status { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Foreign Keys
    public int ProjectId { get; set; }
    public int DataSourceId { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual DataSource DataSource { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = [];
    public virtual ICollection<Annotation> Annotations { get; set; } = [];
    public virtual ICollection<Issue> Issues { get; set; } = [];
}
