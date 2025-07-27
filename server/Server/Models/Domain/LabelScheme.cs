namespace server.Models.Domain;

public class LabelScheme
{
    public int LabelSchemeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Foreign Key to Project
    public int ProjectId { get; set; }

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<Label> Labels { get; set; } = [];
}
