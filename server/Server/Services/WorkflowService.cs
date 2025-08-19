using server.Models.Domain;
using server.Models.DTOs.Workflow;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Data;

namespace server.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowStageService _workflowStageService;
    private readonly IWorkflowStageConnectionService _workflowStageConnectionService;
    private readonly ITaskService _taskService;
    private readonly LaberisDbContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IWorkflowStageService workflowStageService,
        IWorkflowStageConnectionService workflowStageConnectionService,
        ITaskService taskService,
        LaberisDbContext context,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _workflowStageService = workflowStageService ?? throw new ArgumentNullException(nameof(workflowStageService));
        _workflowStageConnectionService = workflowStageConnectionService ?? throw new ArgumentNullException(nameof(workflowStageConnectionService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var workflow = new Workflow
            {
                Name = createDto.Name,
                Description = createDto.Description,
                ProjectId = projectId,
                LabelSchemeId = createDto.LabelSchemeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _workflowRepository.AddAsync(workflow);
            await _workflowRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully created workflow with ID: {WorkflowId}", workflow.WorkflowId);

            // Create workflow stages pipeline using the dedicated service
            var pipelineResult = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
                workflow.WorkflowId, projectId, createDto.CreateDefaultStages, createDto.IncludeReviewStage);
            var initialStageId = pipelineResult.initialStageId;
            var createdStages = pipelineResult.createdStages;

            // Create stage connections for proper workflow progression using the dedicated service
            if (createdStages.Count > 1)
            {
                await _workflowStageConnectionService.CreateStageConnectionsAsync(createdStages);
            }

            // Automatically create tasks for all workflow stages that have assets in their input data sources
            if (createdStages.Count > 0)
            {
                var totalTasksCreated = 0;

                foreach (var stage in createdStages)
                {
                    try
                    {
                        var tasksCreated = await _taskService.CreateTasksForWorkflowStageAsync(projectId, workflow.WorkflowId, stage.Id);
                        totalTasksCreated += tasksCreated;
                        _logger.LogInformation("Automatically created {TasksCreated} tasks for workflow stage {StageId} ({StageName}) in workflow {WorkflowId}",
                            tasksCreated, stage.Id, stage.Name, workflow.WorkflowId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create tasks automatically for workflow stage {StageId} ({StageName}) in workflow {WorkflowId}",
                            stage.Id, stage.Name, workflow.WorkflowId);
                        // Don't fail the workflow creation if task creation fails for individual stages
                    }
                }

                _logger.LogInformation("Automatically created {TotalTasksCreated} total tasks across all stages for workflow {WorkflowId}",
                    totalTasksCreated, workflow.WorkflowId);
            }

            await transaction.CommitAsync();
            return MapToDto(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow for project {ProjectId}", projectId);
            await transaction.RollbackAsync();
            throw;
        }
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

        workflow.Name = updateDto.Name ?? workflow.Name;
        workflow.Description = updateDto.Description ?? workflow.Description;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _workflowRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated workflow with ID: {WorkflowId}", workflowId);

        return MapToDto(workflow);
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

    public async Task<bool> ValidateWorkflowBelongsToProjectAsync(int workflowId, int projectId)
    {
        _logger.LogInformation("Validating if workflow {WorkflowId} belongs to project {ProjectId}", workflowId, projectId);

        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow with ID {WorkflowId} not found", workflowId);
            return false;
        }

        var isValid = workflow.ProjectId == projectId;
        _logger.LogInformation("Workflow {WorkflowId} belongs to project {ProjectId}: {IsValid}", workflowId, projectId, isValid);

        return isValid;
    }

    #region Helper Methods
    private static WorkflowDto MapToDto(Workflow workflow)
    {
        return new WorkflowDto
        {
            Id = workflow.WorkflowId,
            Name = workflow.Name,
            Description = workflow.Description,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            ProjectId = workflow.ProjectId,
            LabelSchemeId = workflow.LabelSchemeId,
            LabelSchemeName = workflow.LabelScheme?.Name,
            StageCount = workflow.WorkflowStages?.Count ?? 0
        };
    }
    #endregion
}
