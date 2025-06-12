using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Label
{
    public record class CreateLabelDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        [StringLength(7)]
        public string? Color { get; init; }
    }
}