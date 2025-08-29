using System.Text.Json.Serialization;

namespace server.Models.DTOs.Export;

/// <summary>
/// Represents the root COCO dataset format for export
/// </summary>
public class CocoDatasetDto
{
    [JsonPropertyName("info")]
    public CocoInfoDto Info { get; set; } = null!;

    [JsonPropertyName("images")]
    public List<CocoImageDto> Images { get; set; } = [];

    [JsonPropertyName("annotations")]
    public List<CocoAnnotationDto> Annotations { get; set; } = [];

    [JsonPropertyName("categories")]
    public List<CocoCategoryDto> Categories { get; set; } = [];
}

/// <summary>
/// COCO dataset information metadata
/// </summary>
public class CocoInfoDto
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("contributor")]
    public string Contributor { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("date_created")]
    public string DateCreated { get; set; } = string.Empty;
}

/// <summary>
/// COCO image representation
/// </summary>
public class CocoImageDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("license")]
    public int? License { get; set; }

    [JsonPropertyName("flickr_url")]
    public string? FlickrUrl { get; set; }

    [JsonPropertyName("coco_url")]
    public string? CocoUrl { get; set; }

    [JsonPropertyName("date_captured")]
    public string? DateCaptured { get; set; }
}

/// <summary>
/// COCO annotation representation
/// </summary>
public class CocoAnnotationDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("image_id")]
    public int ImageId { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("segmentation")]
    public object Segmentation { get; set; } = new object();

    [JsonPropertyName("area")]
    public double Area { get; set; }

    [JsonPropertyName("bbox")]
    public List<double> BBox { get; set; } = [];

    [JsonPropertyName("iscrowd")]
    public int IsCrowd { get; set; } = 0;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }
}

/// <summary>
/// COCO category (label) representation
/// </summary>
public class CocoCategoryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("supercategory")]
    public string SuperCategory { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}