using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.Domain;
using server.Repositories.Interfaces;

namespace server.Repositories;

public class WorkflowStageConnectionRepository : GenericRepository<WorkflowStageConnection>, IWorkflowStageConnectionRepository
{
    private readonly ILogger<WorkflowStageConnectionRepository> _logger;

    public WorkflowStageConnectionRepository(LaberisDbContext context, ILogger<WorkflowStageConnectionRepository> logger) : base(context)
    {
        _logger = logger;
    }

    protected override IQueryable<WorkflowStageConnection> ApplyIncludes(IQueryable<WorkflowStageConnection> query)
    {
        return query
            .Include(c => c.FromStage)
            .Include(c => c.ToStage);
    }

    public async Task<IEnumerable<WorkflowStageConnection>> GetConnectionsForWorkflowAsync(int workflowId)
    {
        _logger.LogInformation("Fetching connections for workflow: {WorkflowId}", workflowId);
        
        return await _context.Set<WorkflowStageConnection>()
            .Include(c => c.FromStage)
            .Include(c => c.ToStage)
            .Where(c => c.FromStage.WorkflowId == workflowId)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkflowStageConnection>> GetIncomingConnectionsAsync(int stageId)
    {
        _logger.LogInformation("Fetching incoming connections for stage: {StageId}", stageId);
        
        return await _context.Set<WorkflowStageConnection>()
            .Include(c => c.FromStage)
            .Include(c => c.ToStage)
            .Where(c => c.ToStageId == stageId)
            .ToListAsync();
    }

    public async Task<IEnumerable<WorkflowStageConnection>> GetOutgoingConnectionsAsync(int stageId)
    {
        _logger.LogInformation("Fetching outgoing connections for stage: {StageId}", stageId);
        
        return await _context.Set<WorkflowStageConnection>()
            .Include(c => c.FromStage)
            .Include(c => c.ToStage)
            .Where(c => c.FromStageId == stageId)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task DeleteConnectionsForStageAsync(int stageId)
    {
        _logger.LogInformation("Deleting all connections for stage: {StageId}", stageId);
        
        var connections = await _context.Set<WorkflowStageConnection>()
            .Where(c => c.FromStageId == stageId || c.ToStageId == stageId)
            .ToListAsync();

        _context.Set<WorkflowStageConnection>().RemoveRange(connections);
    }

    protected override IQueryable<WorkflowStageConnection> ApplyFilter(IQueryable<WorkflowStageConnection> query, string? filterOn, string? filterQuery)
    {
        if (string.IsNullOrWhiteSpace(filterOn) || string.IsNullOrWhiteSpace(filterQuery))
        {
            return query;
        }

        var normalizedFilterOn = filterOn.Trim().ToLowerInvariant();
        var trimmedFilterQuery = filterQuery.Trim();

        switch (normalizedFilterOn)
        {
            case "condition":
                query = query.Where(c => EF.Functions.ILike(c.Condition ?? "", $"%{trimmedFilterQuery}%"));
                break;
            case "from_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var fromStageId))
                {
                    query = query.Where(c => c.FromStageId == fromStageId);
                }
                break;
            case "to_stage_id":
                if (int.TryParse(trimmedFilterQuery, out var toStageId))
                {
                    query = query.Where(c => c.ToStageId == toStageId);
                }
                break;
            default:
                _logger.LogWarning("Unknown filter property: {FilterOn}", filterOn);
                break;
        }
        return query;
    }

    protected override IQueryable<WorkflowStageConnection> ApplySorting(IQueryable<WorkflowStageConnection> query, string? sortBy, bool isAscending)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query.OrderBy(c => c.FromStageId).ThenBy(c => c.ToStageId);
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "created_at" => isAscending ? query.OrderBy(c => c.CreatedAt) : query.OrderByDescending(c => c.CreatedAt),
            "updated_at" => isAscending ? query.OrderBy(c => c.UpdatedAt) : query.OrderByDescending(c => c.UpdatedAt),
            "from_stage_id" => isAscending ? query.OrderBy(c => c.FromStageId) : query.OrderByDescending(c => c.FromStageId),
            "to_stage_id" => isAscending ? query.OrderBy(c => c.ToStageId) : query.OrderByDescending(c => c.ToStageId),
            _ => query.OrderBy(c => c.FromStageId).ThenBy(c => c.ToStageId)
        };
    }
}
