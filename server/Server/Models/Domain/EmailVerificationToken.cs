using System.ComponentModel.DataAnnotations;

namespace server.Models.Domain;

/// <summary>
/// Represents an email verification token for a user registration
/// </summary>
public class EmailVerificationToken
{
    /// <summary>
    /// Primary key for the email verification token
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The verification token string
    /// </summary>
    [Required]
    [StringLength(512)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The user ID associated with this token
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the ApplicationUser
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// The email address this token is for (for validation)
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When this token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this token has been used for verification
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// When this token was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Check if this token is valid (not expired and not used)
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow <= ExpiresAt;
}