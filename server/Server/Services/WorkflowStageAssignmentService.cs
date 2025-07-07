using server.Models.Domain;
using server.Models.DTOs.WorkflowStage;
using server.Models.DTOs.ProjectMember;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class WorkflowStageAssignmentService : IWorkflowStageAssignmentService
{
    private readonly IWorkflowStageAssignmentRepository _assignmentRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly ILogger<WorkflowStageAssignmentService> _logger;

    public WorkflowStageAssignmentService(
        IWorkflowStageAssignmentRepository assignmentRepository,
        IWorkflowStageRepository workflowStageRepository,
        IProjectMemberRepository projectMemberRepository,
        ILogger<WorkflowStageAssignmentService> logger)
    {
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _projectMemberRepository = projectMemberRepository ?? throw new ArgumentNullException(nameof(projectMemberRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<WorkflowStageAssignmentDto>> GetAssignmentsForStageAsync(int workflowStageId)
    {
        _logger.LogInformation("Fetching assignments for workflow stage: {WorkflowStageId}", workflowStageId);

        var assignments = await _assignmentRepository.GetAssignmentsForStageAsync(workflowStageId);
        
        _logger.LogInformation("Fetched {Count} assignments for workflow stage: {WorkflowStageId}", assignments.Count(), workflowStageId);

        return assignments.Select(MapToDto);
    }

    public async Task<WorkflowStageAssignmentDto> CreateAssignmentAsync(CreateWorkflowStageAssignmentDto createDto)
    {
        _logger.LogInformation("Creating assignment for workflow stage {WorkflowStageId} and project member {ProjectMemberId}", 
            createDto.WorkflowStageId, createDto.ProjectMemberId);

        // Validate the assignment
        var isValid = await ValidateAssignmentAsync(createDto.WorkflowStageId, createDto.ProjectMemberId);
        if (!isValid)
        {
            throw new InvalidOperationException($"Cannot assign project member {createDto.ProjectMemberId} to workflow stage {createDto.WorkflowStageId}");
        }

        var assignment = new WorkflowStageAssignment
        {
            WorkflowStageId = createDto.WorkflowStageId,
            ProjectMemberId = createDto.ProjectMemberId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _assignmentRepository.AddAsync(assignment);
        await _assignmentRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created assignment with ID: {AssignmentId}", assignment.WorkflowStageAssignmentId);

        // Fetch the complete assignment with navigation properties
        var createdAssignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignment.WorkflowStageAssignmentId);
        return MapToDto(createdAssignment!);
    }

    public async Task<IEnumerable<WorkflowStageAssignmentDto>> CreateMultipleAssignmentsAsync(int workflowStageId, IEnumerable<int> projectMemberIds)
    {
        _logger.LogInformation("Creating multiple assignments for workflow stage {WorkflowStageId}", workflowStageId);

        var assignments = new List<WorkflowStageAssignment>();
        var memberIds = projectMemberIds.ToList();

        foreach (var projectMemberId in memberIds)
        {
            // Validate each assignment
            var isValid = await ValidateAssignmentAsync(workflowStageId, projectMemberId);
            if (!isValid)
            {
                _logger.LogWarning("Skipping invalid assignment: workflow stage {WorkflowStageId}, project member {ProjectMemberId}", 
                    workflowStageId, projectMemberId);
                continue;
            }

            var assignment = new WorkflowStageAssignment
            {
                WorkflowStageId = workflowStageId,
                ProjectMemberId = projectMemberId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            assignments.Add(assignment);
        }

        if (assignments.Count != 0)
        {
            await _assignmentRepository.AddRangeAsync(assignments);
            await _assignmentRepository.SaveChangesAsync();
        }

        _logger.LogInformation("Successfully created {Count} assignments for workflow stage {WorkflowStageId}", 
            assignments.Count, workflowStageId);

        // Fetch the complete assignments with navigation properties
        var createdAssignments = new List<WorkflowStageAssignmentDto>();
        // TODO: Consider optimizing this to reduce database calls
        // Fetch assignments in bulk if possible
        foreach (var assignment in assignments)
        {
            var createdAssignment = await _assignmentRepository.GetByIdWithDetailsAsync(assignment.WorkflowStageAssignmentId);
            if (createdAssignment != null)
            {
                createdAssignments.Add(MapToDto(createdAssignment));
            }
        }

        return createdAssignments;
    }

    public async Task<bool> DeleteAssignmentAsync(int assignmentId)
    {
        _logger.LogInformation("Deleting assignment with ID: {AssignmentId}", assignmentId);

        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);
        if (assignment == null)
        {
            _logger.LogWarning("Assignment with ID: {AssignmentId} not found for deletion.", assignmentId);
            return false;
        }

        _assignmentRepository.Remove(assignment);
        await _assignmentRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted assignment with ID: {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<int> DeleteAllAssignmentsForStageAsync(int workflowStageId)
    {
        _logger.LogInformation("Deleting all assignments for workflow stage: {WorkflowStageId}", workflowStageId);

        var assignments = await _assignmentRepository.FindAsync(a => a.WorkflowStageId == workflowStageId);
        var assignmentsList = assignments.ToList();

        if (assignmentsList.Any())
        {
            _assignmentRepository.RemoveRange(assignmentsList);
            await _assignmentRepository.SaveChangesAsync();
        }

        _logger.LogInformation("Successfully deleted {Count} assignments for workflow stage: {WorkflowStageId}", 
            assignmentsList.Count, workflowStageId);

        return assignmentsList.Count;
    }

    public async Task<bool> ValidateAssignmentAsync(int workflowStageId, int projectMemberId)
    {
        _logger.LogInformation("Validating assignment: workflow stage {WorkflowStageId}, project member {ProjectMemberId}", 
            workflowStageId, projectMemberId);

        // Check if workflow stage exists
        var workflowStage = await _workflowStageRepository.GetByIdAsync(workflowStageId);
        if (workflowStage == null)
        {
            _logger.LogWarning("Workflow stage with ID {WorkflowStageId} not found", workflowStageId);
            return false;
        }

        // Check if project member exists
        var projectMember = await _projectMemberRepository.GetByIdAsync(projectMemberId);
        if (projectMember == null)
        {
            _logger.LogWarning("Project member with ID {ProjectMemberId} not found", projectMemberId);
            return false;
        }

        // Check if the project member belongs to the same project as the workflow
        if (projectMember.ProjectId != workflowStage.Workflow.ProjectId)
        {
            _logger.LogWarning("Project member {ProjectMemberId} does not belong to the same project as workflow stage {WorkflowStageId}", 
                projectMemberId, workflowStageId);
            return false;
        }

        // Check if assignment already exists
        var existingAssignment = await _assignmentRepository.FindAsync(a => 
            a.WorkflowStageId == workflowStageId && a.ProjectMemberId == projectMemberId);
        
        if (existingAssignment.Any())
        {
            _logger.LogWarning("Assignment already exists for workflow stage {WorkflowStageId} and project member {ProjectMemberId}", 
                workflowStageId, projectMemberId);
            return false;
        }

        _logger.LogInformation("Assignment validation passed for workflow stage {WorkflowStageId} and project member {ProjectMemberId}", 
            workflowStageId, projectMemberId);
        return true;
    }

    private static WorkflowStageAssignmentDto MapToDto(WorkflowStageAssignment assignment)
    {
        return new WorkflowStageAssignmentDto
        {
            Id = assignment.WorkflowStageAssignmentId,
            WorkflowStageId = assignment.WorkflowStageId,
            ProjectMember = new ProjectMemberDto
            {
                Id = assignment.ProjectMember.ProjectMemberId,
                Role = assignment.ProjectMember.Role,
                InvitedAt = assignment.ProjectMember.InvitedAt,
                JoinedAt = assignment.ProjectMember.JoinedAt,
                CreatedAt = assignment.ProjectMember.CreatedAt,
                UpdatedAt = assignment.ProjectMember.UpdatedAt,
                ProjectId = assignment.ProjectMember.ProjectId,
                Email = assignment.ProjectMember.User?.Email ?? "",
                UserName = assignment.ProjectMember.User?.UserName
            },
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}
