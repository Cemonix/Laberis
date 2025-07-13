using System;
using System.ComponentModel.DataAnnotations;

namespace server.Configs;

public record WebAppSettings
{
    public const string SectionName = "WebApp";
    
    [Required(AllowEmptyStrings = false, ErrorMessage = "WebApp:ClientUrl is required and cannot be empty.")]
    [Url(ErrorMessage = "WebApp:ClientUrl must be a valid URL.")]
    public string ClientUrl { get; init; } = string.Empty;
}
