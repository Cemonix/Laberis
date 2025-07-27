namespace server.Models.Domain;

public class Label
{
    public int LabelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public string? OriginalName { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Foreign Key to LabelScheme
    public int LabelSchemeId { get; set; }

    // Navigation Properties
    public virtual LabelScheme LabelScheme { get; set; } = null!;
    public virtual ICollection<Annotation> Annotations { get; set; } = [];
}
