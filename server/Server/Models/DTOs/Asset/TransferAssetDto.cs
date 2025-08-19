using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.Asset;

/// <summary>
/// DTO for transferring an asset to a different data source.
/// </summary>
public class TransferAssetDto
{
    /// <summary>
    /// The ID of the target data source to transfer the asset to.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Target data source ID must be a positive integer.")]
    public int TargetDataSourceId { get; set; }
}