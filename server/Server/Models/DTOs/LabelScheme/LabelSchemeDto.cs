namespace server.Models.DTOs.LabelScheme;

public record class LabelSchemeDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsDefault { get; init; }
    public int ProjectId { get; init; }
    public DateTime CreatedAt { get; init; }
}
