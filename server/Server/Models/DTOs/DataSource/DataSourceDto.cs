using server.Models.Domain.Enums;

namespace server.Models.DTOs.DataSource;

public record class DataSourceDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DataSourceType SourceType { get; init; }
    public DataSourceStatus Status { get; init; }
    public bool IsDefault { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ProjectId { get; init; }
    public int AssetCount { get; init; }
}
