namespace server.Models.Domain;

public record class Label
{
    public int LabelId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Color { get; init; }
    public string? Description { get; init; }
    public string? Metadata { get; init; }
    public string? OriginalName { get; init; }
    public bool IsActive { get; init; } = true;

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }

    // Foreign Key to LabelScheme
    public int LabelSchemeId { get; init; }

    // Navigation Properties
    public virtual LabelScheme LabelScheme { get; init; } = null!;
    public virtual ICollection<Annotation> Annotations { get; init; } = [];
}
