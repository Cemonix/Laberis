namespace server.Models.DTOs.Task;

/// <summary>
/// DTO for task veto requests
/// </summary>
public class VetoTaskDto
{
    /// <summary>
    /// Reason for vetoing the task (required for veto operations)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}