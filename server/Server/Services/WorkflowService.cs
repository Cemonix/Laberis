using server.Models.Domain;
using server.Models.DTOs.Workflow;
using server.Models.DTOs.WorkflowStage;
using server.Models.Common;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using System.Threading.Tasks;

namespace server.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowStageService _workflowStageService;
    private readonly IWorkflowStageAssignmentService _workflowStageAssignmentService;
    private readonly ITaskService _taskService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IWorkflowStageService workflowStageService,
        IWorkflowStageAssignmentService workflowStageAssignmentService,
        ITaskService taskService,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _workflowStageService = workflowStageService ?? throw new ArgumentNullException(nameof(workflowStageService));
        _workflowStageAssignmentService = workflowStageAssignmentService ?? throw new ArgumentNullException(nameof(workflowStageAssignmentService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
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

        // Create workflow stages
        var initialStageId = await CreateWorkflowStagesAsync(workflow.WorkflowId, createDto);

        // Automatically create tasks for all available assets if an initial stage was created
        if (initialStageId.HasValue)
        {
            try
            {
                var tasksCreated = await _taskService.CreateTasksForAllAssetsAsync(projectId, workflow.WorkflowId, initialStageId.Value);
                _logger.LogInformation("Automatically created {TasksCreated} tasks for workflow {WorkflowId}", tasksCreated, workflow.WorkflowId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create tasks automatically for workflow {WorkflowId}", workflow.WorkflowId);
                // Don't fail the workflow creation if task creation fails
            }
        }

        return MapToDto(workflow);
    }

    private async Task<int?> CreateWorkflowStagesAsync(int workflowId, CreateWorkflowDto createDto)
    {
        var stageOrder = 1;
        int? initialStageId = null;

        // Create default stages if requested
        if (createDto.CreateDefaultStages)
        {
            _logger.LogInformation("Creating default stages for workflow: {WorkflowId}", workflowId);

            // Annotation stage (always created first if default stages requested)
            var annotationStage = new CreateWorkflowStageDto
            {
                Name = "Annotation",
                Description = "Initial annotation stage",
                StageType = WorkflowStageType.ANNOTATION,
                StageOrder = stageOrder++,
                IsInitialStage = true,
                IsFinalStage = false
            };
            var createdAnnotationStage = await _workflowStageService.CreateWorkflowStageAsync(workflowId, annotationStage);
            initialStageId = createdAnnotationStage.Id; // This is the initial stage

            // Revision stage (if review stage is requested)
            if (createDto.IncludeReviewStage)
            {
                var revisionStage = new CreateWorkflowStageDto
                {
                    Name = "Revision",
                    Description = "Review and revision stage",
                    StageType = WorkflowStageType.REVISION,
                    StageOrder = stageOrder++,
                    IsInitialStage = false,
                    IsFinalStage = false
                };
                await _workflowStageService.CreateWorkflowStageAsync(workflowId, revisionStage);
            }

            // Completion stage (always created last if default stages requested)
            var completionStage = new CreateWorkflowStageDto
            {
                Name = "Completion",
                Description = "Final completion stage",
                StageType = WorkflowStageType.COMPLETION,
                StageOrder = stageOrder++,
                IsInitialStage = false,
                IsFinalStage = true
            };
            await _workflowStageService.CreateWorkflowStageAsync(workflowId, completionStage);
        }

        // Create custom stages if provided
        if (createDto.Stages != null && createDto.Stages.Count != 0)
        {
            _logger.LogInformation("Creating {StageCount} custom stages for workflow: {WorkflowId}",
                createDto.Stages.Count, workflowId);

            foreach (var stageDto in createDto.Stages)
            {
                // Create the stage
                var createStageDto = new CreateWorkflowStageDto
                {
                    Name = stageDto.Name,
                    Description = stageDto.Description,
                    StageType = stageDto.StageType,
                    StageOrder = stageDto.StageOrder > 0 ? stageDto.StageOrder : stageOrder++,
                    IsInitialStage = stageDto.IsInitialStage,
                    IsFinalStage = stageDto.IsFinalStage,
                    InputDataSourceId = stageDto.InputDataSourceId,
                    TargetDataSourceId = stageDto.TargetDataSourceId
                };

                var createdStage = await _workflowStageService.CreateWorkflowStageAsync(workflowId, createStageDto);

                // Track the initial stage
                if (stageDto.IsInitialStage)
                {
                    initialStageId = createdStage.Id;
                }

                // Create assignments for this stage if provided
                if (stageDto.AssignedProjectMemberIds != null && stageDto.AssignedProjectMemberIds.Count != 0)
                {
                    _logger.LogInformation("Creating {AssignmentCount} assignments for stage: {StageId}",
                        stageDto.AssignedProjectMemberIds.Count, createdStage.Id);

                    await _workflowStageAssignmentService.CreateMultipleAssignmentsAsync(
                        createdStage.Id, stageDto.AssignedProjectMemberIds);
                }
            }
        }

        return initialStageId;
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
