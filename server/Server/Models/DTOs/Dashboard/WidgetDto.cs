namespace server.Models.DTOs.Dashboard;

/// <summary>
/// DTO for widget configuration and metadata
/// </summary>
public class WidgetDefinitionDto
{
    public string WidgetType { get; set; } = string.Empty; // "workflow_progress", "user_performance", etc.
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DefaultWidth { get; set; } = 6; // Grid columns (1-12)
    public int DefaultHeight { get; set; } = 4; // Grid rows
    public bool RequiresConfiguration { get; set; } = false;
    public List<WidgetSettingDto> AvailableSettings { get; set; } = [];
    public List<string> SupportedRoles { get; set; } = []; // Which project roles can use this widget
}

/// <summary>
/// DTO for widget instance configuration
/// </summary>
public class WidgetInstanceDto
{
    public string WidgetId { get; set; } = string.Empty; // Unique identifier for this instance
    public string WidgetType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int GridX { get; set; } = 0;
    public int GridY { get; set; } = 0;
    public int GridWidth { get; set; } = 6;
    public int GridHeight { get; set; } = 4;
    public bool IsVisible { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = [];
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// DTO for widget settings configuration
/// </summary>
public class WidgetSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "select", "text", "number", "boolean", "date_range"
    public object? DefaultValue { get; set; }
    public List<WidgetSettingOptionDto>? Options { get; set; } // For select type
    public bool Required { get; set; } = false;
    public string? ValidationRegex { get; set; }
    public string? HelpText { get; set; }
}

/// <summary>
/// DTO for widget setting options (for select type settings)
/// </summary>
public class WidgetSettingOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// DTO for dashboard layout configuration
/// </summary>
public class DashboardLayoutDto
{
    public List<WidgetInstanceDto> Widgets { get; set; } = [];
    public string Theme { get; set; } = "light"; // "light" or "dark"
    public int RefreshIntervalSeconds { get; set; } = 300; // Auto-refresh interval
    public DateTime LastModified { get; set; }
}

/// <summary>
/// DTO for widget data request with filters
/// </summary>
public class WidgetDataRequestDto
{
    public string WidgetType { get; set; } = string.Empty;
    public int? WorkflowId { get; set; }
    public int? WorkflowStageId { get; set; }
    public string? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? Limit { get; set; } = 50;
    public Dictionary<string, object> CustomFilters { get; set; } = [];
}

/// <summary>
/// DTO for widget data response
/// </summary>
public class WidgetDataDto
{
    public string WidgetType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
}