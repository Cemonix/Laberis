namespace server.Models.DTOs.LabelScheme;

public record LabelSchemeDeletionImpactDto
{
    public int LabelSchemeId { get; init; }
    public string LabelSchemeName { get; init; } = string.Empty;
    public int TotalLabelsCount { get; init; }
    public int TotalAnnotationsCount { get; init; }
    public IEnumerable<LabelImpactDto> LabelImpacts { get; init; } = [];
}

public record LabelImpactDto
{
    public int LabelId { get; init; }
    public string LabelName { get; init; } = string.Empty;
    public string? LabelColor { get; init; }
    public int AnnotationsCount { get; init; }
}