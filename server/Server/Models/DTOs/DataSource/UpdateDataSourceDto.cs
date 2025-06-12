using server.Models.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.DataSource
{
    public record UpdateDataSourceDto
    {
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        [Required]
        public DataSourceStatus Status { get; init; }

        public string? ConnectionDetails { get; init; }
    }
}