using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public class MinioSettings
{
    public const string SectionName = "Minio";

    [Required(AllowEmptyStrings = false)]
    public string Endpoint { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;
}
