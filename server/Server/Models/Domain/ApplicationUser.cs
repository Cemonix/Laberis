using Microsoft.AspNetCore.Identity;
using server.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public virtual ICollection<WorkflowStage> WorkflowStages { get; init; } = [];
}