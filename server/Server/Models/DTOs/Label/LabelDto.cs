namespace server.Models.DTOs.Label
{
    public record class LabelDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? Color { get; init; }
        public int LabelSchemeId { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}