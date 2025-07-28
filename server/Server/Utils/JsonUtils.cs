using System.Text.Json;

namespace server.Utils;

/// <summary>
/// Utility class for JSON operations and validation
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Validates if a string is valid JSON
    /// </summary>
    /// <param name="jsonString">The string to validate</param>
    /// <returns>True if valid JSON, false otherwise</returns>
    public static bool IsValidJson(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is valid JSON or null/empty (which is acceptable for nullable JSON fields)
    /// </summary>
    /// <param name="jsonString">The string to validate</param>
    /// <returns>True if valid JSON or null/empty, false if invalid JSON</returns>
    public static bool IsValidJsonOrNull(string? jsonString)
    {
        return string.IsNullOrWhiteSpace(jsonString) || IsValidJson(jsonString);
    }

    /// <summary>
    /// Ensures a string is valid JSON by returning null if invalid
    /// </summary>
    /// <param name="jsonString">The string to validate</param>
    /// <returns>The original string if valid JSON, null if invalid</returns>
    public static string? EnsureValidJsonOrNull(string? jsonString)
    {
        return IsValidJsonOrNull(jsonString) ? jsonString : null;
    }

    /// <summary>
    /// Ensures a string is valid JSON by returning an empty JSON object if invalid or null
    /// </summary>
    /// <param name="jsonString">The string to validate</param>
    /// <returns>The original string if valid JSON, "{}" if invalid or null</returns>
    public static string EnsureValidJsonOrEmpty(string? jsonString)
    {
        return IsValidJson(jsonString) ? jsonString! : "{}";
    }
}