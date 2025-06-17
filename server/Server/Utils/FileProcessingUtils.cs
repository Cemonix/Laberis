using Microsoft.AspNetCore.StaticFiles;
using SixLabors.ImageSharp;

namespace server.Utils;

/// <summary>
/// Utility class for file processing operations using built-in .NET capabilities and ImageSharp
/// </summary>
public static class FileProcessingUtils
{
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    /// <summary>
    /// Allowed image file extensions for upload
    /// </summary>
    public static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
    };

    /// <summary>
    /// Maximum file size in bytes (100MB)
    /// </summary>
    public const long MaxFileSizeBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Validates if a file is a supported image format
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <param name="contentType">The content type</param>
    /// <returns>True if the file is a supported image format</returns>
    public static bool IsValidImageFile(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName);
        if (!AllowedImageExtensions.Contains(extension))
        {
            return false;
        }

        // Additional MIME type validation
        if (!string.IsNullOrEmpty(contentType))
        {
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    /// <summary>
    /// Gets the MIME type for a file based on its extension
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <returns>The MIME type</returns>
    public static string GetMimeType(string fileName)
    {
        if (_contentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            return contentType;
        }

        return "application/octet-stream";
    }

    /// <summary>
    /// Generates a unique file name to avoid conflicts
    /// </summary>
    /// <param name="originalFileName">The original file name</param>
    /// <param name="projectId">The project ID</param>
    /// <returns>A unique file name</returns>
    public static string GenerateUniqueFileName(string originalFileName, int projectId)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");
        var guid = Guid.NewGuid().ToString("N")[..8]; // First 8 characters
        
        return $"{projectId}/{timestamp}_{guid}_{nameWithoutExtension}{extension}";
    }

    /// <summary>
    /// Sanitizes a file name by removing invalid characters
    /// </summary>
    /// <param name="fileName">The file name to sanitize</param>
    /// <returns>A sanitized file name</returns>
    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Limit length and ensure it's not empty
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "unnamed_file";
        }

        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }

    /// <summary>
    /// Extracts image dimensions and metadata using ImageSharp
    /// This provides robust support for all common image formats
    /// </summary>
    /// <param name="imageStream">The image stream</param>
    /// <returns>The image dimensions, or null if not a valid image</returns>
    public static (int Width, int Height)? GetImageDimensions(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            
            using var image = Image.Load(imageStream);
            return (image.Width, image.Height);
        }
        catch (Exception)
        {
            // If ImageSharp can't load it, it's not a valid image
            return null;
        }
        finally
        {
            imageStream.Position = 0; // Reset stream position
        }
    }

    /// <summary>
    /// Validates that a stream contains a valid image using ImageSharp
    /// This is more reliable than just checking file extensions
    /// </summary>
    /// <param name="imageStream">The image stream</param>
    /// <returns>True if the stream contains a valid image</returns>
    public static bool IsValidImageStream(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            
            // Try to identify the image format
            var format = Image.DetectFormat(imageStream);
            return format != null;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            imageStream.Position = 0;
        }
    }

    /// <summary>
    /// Extracts comprehensive image metadata using ImageSharp
    /// </summary>
    /// <param name="imageStream">The image stream</param>
    /// <returns>Image metadata including format, dimensions, and EXIF data</returns>
    public static ImageMetadata? ExtractImageMetadata(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            
            using var image = Image.Load(imageStream);
            
            var exifData = new Dictionary<string, object>();

            // Extract EXIF data if available
            if (image.Metadata.ExifProfile != null)
            {
                foreach (var value in image.Metadata.ExifProfile.Values)
                {
                    try
                    {
                        var tagName = value.Tag.ToString();
                        var tagValue = value.GetValue();
                        
                        if (tagValue != null)
                        {
                            exifData[tagName] = tagValue;
                        }
                    }
                    catch
                    {
                        // Skip problematic EXIF values
                    }
                }
            }

            var metadata = new ImageMetadata
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
                ExifData = exifData
            };

            return metadata;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            imageStream.Position = 0;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility - now uses ImageSharp internally
    /// </summary>
    /// <param name="imageStream">The image stream</param>
    /// <returns>The image dimensions, or null if not readable</returns>
    [Obsolete("Use GetImageDimensions instead")]
    public static (int Width, int Height)? TryGetImageDimensions(Stream imageStream)
    {
        return GetImageDimensions(imageStream);
    }
}

/// <summary>
/// Comprehensive image metadata extracted using ImageSharp
/// </summary>
public record ImageMetadata
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = string.Empty;
    public Dictionary<string, object> ExifData { get; init; } = new();
}
