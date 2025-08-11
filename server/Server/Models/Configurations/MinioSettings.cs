using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public record MinioSettings
{
    public const string SectionName = "Minio";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; init; } = string.Empty;

    public bool UseSsl { get; init; } = true;
}
