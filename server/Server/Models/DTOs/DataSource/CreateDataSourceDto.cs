using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.DataSource
{
    public record CreateDataSourceDto
    {
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        [Required]
        public DataSourceType SourceType { get; init; }

        // Represents the JSON string for connection details
        public string? ConnectionDetails { get; init; }
    }
}