using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;

namespace server.Repositories;

public class WorkflowStageAssignmentRepository : GenericRepository<WorkflowStageAssignment>, IWorkflowStageAssignmentRepository
{
    private readonly ILogger<WorkflowStageAssignmentRepository> _logger;

    public WorkflowStageAssignmentRepository(LaberisDbContext context, ILogger<WorkflowStageAssignmentRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<WorkflowStageAssignment> ApplyIncludes(IQueryable<WorkflowStageAssignment> query)
    {
        return query
            .Include(wsa => wsa.WorkflowStage)
                .ThenInclude(ws => ws.Workflow)
            .Include(wsa => wsa.ProjectMember)
                .ThenInclude(pm => pm.User);
    }

    protected override IQueryable<WorkflowStageAssignment> ApplyFilter(IQueryable<WorkflowStageAssignment> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "workflow_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var workflowStageId))
                {
                    query = query.Where(wsa => wsa.WorkflowStageId == workflowStageId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow stage ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "project_member_id":
                if (int.TryParse(trimmedFilterQuery, out var projectMemberId))
                {
                    query = query.Where(wsa => wsa.ProjectMemberId == projectMemberId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse project member ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            case "user_email":
                query = query.Where(wsa => EF.Functions.ILike(wsa.ProjectMember.User!.Email!, $"%{trimmedFilterQuery}%"));
                break;
            case "workflow_id":
                if (int.TryParse(trimmedFilterQuery, out var workflowId))
                {
                    query = query.Where(wsa => wsa.WorkflowStage.WorkflowId == workflowId);
                }
                else
                {
                    _logger.LogWarning("Failed to parse workflow ID: {TrimmedFilterQuery}", trimmedFilterQuery);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<WorkflowStageAssignment> ApplySorting(IQueryable<WorkflowStageAssignment> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderBy(wsa => wsa.WorkflowStage.StageOrder)
                       .ThenBy(wsa => wsa.ProjectMember.User!.Email);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "created_at" => isAscending ? query.OrderBy(wsa => wsa.CreatedAt) : query.OrderByDescending(wsa => wsa.CreatedAt),
            "updated_at" => isAscending ? query.OrderBy(wsa => wsa.UpdatedAt) : query.OrderByDescending(wsa => wsa.UpdatedAt),
            "workflow_stage_id" => isAscending ? query.OrderBy(wsa => wsa.WorkflowStageId) : query.OrderByDescending(wsa => wsa.WorkflowStageId),
            "project_member_id" => isAscending ? query.OrderBy(wsa => wsa.ProjectMemberId) : query.OrderByDescending(wsa => wsa.ProjectMemberId),
            "stage_order" => isAscending ? query.OrderBy(wsa => wsa.WorkflowStage.StageOrder) : query.OrderByDescending(wsa => wsa.WorkflowStage.StageOrder),
            "user_email" => isAscending ? query.OrderBy(wsa => wsa.ProjectMember.User!.Email) : query.OrderByDescending(wsa => wsa.ProjectMember.User!.Email),
            _ => query.OrderBy(wsa => wsa.WorkflowStage.StageOrder).ThenBy(wsa => wsa.ProjectMember.User!.Email)
        };
    }

    public async Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForStageAsync(int workflowStageId)
    {
        _logger.LogInformation("Fetching assignments for workflow stage: {WorkflowStageId}", workflowStageId);
        
        return await _context.Set<WorkflowStageAssignment>()
            .Include(wsa => wsa.ProjectMember)
                .ThenInclude(pm => pm.User)
            .Where(wsa => wsa.WorkflowStageId == workflowStageId)
            .ToListAsync();
    }

    public async Task<WorkflowStageAssignment?> GetByIdWithDetailsAsync(int assignmentId)
    {
        _logger.LogInformation("Fetching assignment with details: {AssignmentId}", assignmentId);
        
        return await _context.Set<WorkflowStageAssignment>()
            .Include(wsa => wsa.WorkflowStage)
                .ThenInclude(ws => ws.Workflow)
            .Include(wsa => wsa.ProjectMember)
                .ThenInclude(pm => pm.User)
            .FirstOrDefaultAsync(wsa => wsa.WorkflowStageAssignmentId == assignmentId);
    }

    public async Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForProjectMemberAsync(int projectMemberId)
    {
        _logger.LogInformation("Fetching assignments for project member: {ProjectMemberId}", projectMemberId);
        
        return await _context.Set<WorkflowStageAssignment>()
            .Include(wsa => wsa.WorkflowStage)
                .ThenInclude(ws => ws.Workflow)
            .Where(wsa => wsa.ProjectMemberId == projectMemberId)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkflowStageAssignment>> GetAssignmentsForWorkflowAsync(int workflowId)
    {
        _logger.LogInformation("Fetching assignments for workflow: {WorkflowId}", workflowId);
        
        return await _context.Set<WorkflowStageAssignment>()
            .Include(wsa => wsa.WorkflowStage)
            .Include(wsa => wsa.ProjectMember)
                .ThenInclude(pm => pm.User)
            .Where(wsa => wsa.WorkflowStage.WorkflowId == workflowId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task AddRangeAsync(IEnumerable<WorkflowStageAssignment> assignments)
    {
        await _context.Set<WorkflowStageAssignment>().AddRangeAsync(assignments);
    }

    public void RemoveRange(IEnumerable<WorkflowStageAssignment> assignments)
    {
        _context.Set<WorkflowStageAssignment>().RemoveRange(assignments);
    }
}
