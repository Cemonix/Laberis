using server.Models.DTOs.ProjectMember;

namespace server.Models.DTOs.WorkflowStage;

public record class WorkflowStageAssignmentDto
{
    public int Id { get; init; }
    public int WorkflowStageId { get; init; }
    public ProjectMemberDto ProjectMember { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
