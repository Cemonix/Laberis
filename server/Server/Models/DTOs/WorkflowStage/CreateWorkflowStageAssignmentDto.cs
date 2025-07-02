using System.ComponentModel.DataAnnotations;

namespace server.Models.DTOs.WorkflowStage;

public record class CreateWorkflowStageAssignmentDto
{
    [Required]
    public int WorkflowStageId { get; init; }

    [Required]
    public int ProjectMemberId { get; init; }
}
