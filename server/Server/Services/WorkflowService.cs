using server.Models.Domain;
using server.Models.DTOs.Workflow;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<WorkflowDto>> GetWorkflowsForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching workflows for project: {ProjectId}", projectId);

        var (workflows, totalCount) = await _workflowRepository.GetAllWithCountAsync(
            filter: w => w.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} workflows for project: {ProjectId}", workflows.Count(), projectId);

        var workflowDtos = workflows.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<WorkflowDto>
        {
            Data = workflowDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<WorkflowDto?> GetWorkflowByIdAsync(int workflowId)
    {
        _logger.LogInformation("Fetching workflow with ID: {WorkflowId}", workflowId);
        
        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID: {WorkflowId} not found.", workflowId);
            return null;
        }

        return MapToDto(workflow);
    }

    public async Task<WorkflowDto> CreateWorkflowAsync(int projectId, CreateWorkflowDto createDto)
    {
        _logger.LogInformation("Creating new workflow for project: {ProjectId}", projectId);

        var workflow = new Workflow
        {
            Name = createDto.Name,
            Description = createDto.Description,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workflowRepository.AddAsync(workflow);
        await _workflowRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created workflow with ID: {WorkflowId}", workflow.WorkflowId);
        
        return MapToDto(workflow);
    }

    public async Task<WorkflowDto?> UpdateWorkflowAsync(int workflowId, UpdateWorkflowDto updateDto)
    {
        _logger.LogInformation("Updating workflow with ID: {WorkflowId}", workflowId);

        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID: {WorkflowId} not found for update.", workflowId);
            return null;
        }

        var updatedWorkflow = workflow with
        {
            Name = updateDto.Name,
            Description = updateDto.Description,
            UpdatedAt = DateTime.UtcNow
        };

        _workflowRepository.Update(updatedWorkflow);
        await _workflowRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated workflow with ID: {WorkflowId}", workflowId);
        
        return MapToDto(updatedWorkflow);
    }

    public async Task<bool> DeleteWorkflowAsync(int workflowId)
    {
        _logger.LogInformation("Deleting workflow with ID: {WorkflowId}", workflowId);

        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID: {WorkflowId} not found for deletion.", workflowId);
            return false;
        }

        _workflowRepository.Remove(workflow);
        await _workflowRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted workflow with ID: {WorkflowId}", workflowId);
        
        return true;
    }

    private static WorkflowDto MapToDto(Workflow workflow)
    {
        return new WorkflowDto
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Description = workflow.Description,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            ProjectId = workflow.ProjectId
        };
    }
}
