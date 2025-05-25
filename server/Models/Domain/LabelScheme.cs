namespace server.Models.Domain;

public record class LabelScheme
{
    public int LabelSchemeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    // Foreign Key to Project
    public int ProjectId { get; init; }

    // Navigation Properties
    public virtual Project Project { get; init; } = null!;
    public virtual ICollection<Label> Labels { get; init; } = [];
}
