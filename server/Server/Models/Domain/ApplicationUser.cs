using Microsoft.AspNetCore.Identity;
using server.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public virtual ICollection<WorkflowStage> WorkflowStages { get; init; } = [];
}