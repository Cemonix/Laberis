using server.Models.Domain;
using server.Models.DTOs.Workflow;
using server.Models.DTOs.WorkflowStage;
using server.Models.DTOs.DataSource;
using server.Models.Common;
using server.Models.Domain.Enums;
using server.Models.Internal;
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
    private readonly IDataSourceService _dataSourceService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        IWorkflowRepository workflowRepository,
        IWorkflowStageService workflowStageService,
        IWorkflowStageAssignmentService workflowStageAssignmentService,
        ITaskService taskService,
        IDataSourceService dataSourceService,
        ILogger<WorkflowService> logger)
    {
        _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        _workflowStageService = workflowStageService ?? throw new ArgumentNullException(nameof(workflowStageService));
        _workflowStageAssignmentService = workflowStageAssignmentService ?? throw new ArgumentNullException(nameof(workflowStageAssignmentService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
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

        // Get project ID to find/create data sources
        var workflow = await _workflowRepository.GetByIdAsync(workflowId);
        if (workflow == null)
        {
            _logger.LogError("Workflow {WorkflowId} not found when creating stages", workflowId);
            return null;
        }

        var projectId = workflow.ProjectId;

        // Create default stages if requested
        if (createDto.CreateDefaultStages)
        {
            _logger.LogInformation("Creating default stages with proper data source connections for workflow: {WorkflowId}", workflowId);

            // Get or ensure required data sources exist
            var dataSources = await EnsureRequiredDataSourcesExistAsync(projectId, createDto.IncludeReviewStage);
            
            if (dataSources.AnnotationDataSource == null)
            {
                _logger.LogError("No annotation data source available for project {ProjectId}", projectId);
                return null;
            }

            // Create Annotation Stage (connected to default/annotation data source)
            var annotationStage = new CreateWorkflowStageDto
            {
                Name = "Annotation",
                Description = "Initial annotation stage for labeling assets",
                StageType = WorkflowStageType.ANNOTATION,
                StageOrder = stageOrder++,
                IsInitialStage = true,
                IsFinalStage = !createDto.IncludeReviewStage, // If no review stage, this is final
                InputDataSourceId = dataSources.AnnotationDataSource.Id,
                TargetDataSourceId = createDto.IncludeReviewStage ? dataSources.ReviewDataSource?.Id : dataSources.CompletionDataSource?.Id
            };
            var createdAnnotationStage = await _workflowStageService.CreateWorkflowStageAsync(workflowId, annotationStage);
            initialStageId = createdAnnotationStage.Id; // This is where tasks are first created

            // Create Review Stage (if requested)
            if (createDto.IncludeReviewStage && dataSources.ReviewDataSource != null)
            {
                var revisionStage = new CreateWorkflowStageDto
                {
                    Name = "Review",
                    Description = "Review and quality control stage",
                    StageType = WorkflowStageType.REVISION,
                    StageOrder = stageOrder++,
                    IsInitialStage = false,
                    IsFinalStage = false,
                    InputDataSourceId = dataSources.ReviewDataSource.Id,
                    TargetDataSourceId = dataSources.CompletionDataSource?.Id
                };
                await _workflowStageService.CreateWorkflowStageAsync(workflowId, revisionStage);
            }

            // Create Completion Stage (final stage)
            if (dataSources.CompletionDataSource != null)
            {
                var completionStage = new CreateWorkflowStageDto
                {
                    Name = "Completion",
                    Description = "Final completion and export stage",
                    StageType = WorkflowStageType.COMPLETION,
                    StageOrder = stageOrder++,
                    IsInitialStage = false,
                    IsFinalStage = true,
                    InputDataSourceId = dataSources.CompletionDataSource.Id,
                    TargetDataSourceId = null // Final stage - no target needed
                };
                await _workflowStageService.CreateWorkflowStageAsync(workflowId, completionStage);
            }

            _logger.LogInformation("Successfully created default workflow stages with data source connections for workflow {WorkflowId}", workflowId);
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

    /// <summary>
    /// Ensures that the required data sources exist for the workflow stages.
    /// Creates missing data sources automatically to support the complete workflow pipeline.
    /// </summary>
    private async Task<WorkflowDataSources> EnsureRequiredDataSourcesExistAsync(int projectId, bool includeReviewStage)
    {
        _logger.LogInformation("Ensuring required data sources exist for project {ProjectId}, includeReview: {IncludeReview}", 
            projectId, includeReviewStage);

        // Get all existing data sources for the project
        var existingDataSources = await _dataSourceService.GetAllDataSourcesForProjectAsync(
            projectId, pageNumber: 1, pageSize: 100); // Get all data sources

        var result = new WorkflowDataSources
        {
            // Find or create annotation data source (look for default first)
            AnnotationDataSource = existingDataSources.Data.FirstOrDefault(ds => ds.IsDefault)
                ?? existingDataSources.Data.FirstOrDefault()
        };

        if (result.AnnotationDataSource == null)
        {
            _logger.LogWarning("No data sources exist for project {ProjectId} - creating default annotation data source", projectId);
            
            result.AnnotationDataSource = await _dataSourceService.CreateDataSourceAsync(projectId, new CreateDataSourceDto
            {
                Name = "Default Annotation Source",
                Description = "Default data source for annotation assets",
                SourceType = DataSourceType.MINIO_BUCKET
            });
        }

        // Create or find review data source (if review stage requested)
        if (includeReviewStage)
        {
            result.ReviewDataSource = existingDataSources.Data.FirstOrDefault(ds =>
                ds.Name.Contains("review", StringComparison.CurrentCultureIgnoreCase)
                || ds.Name.Contains("revision", StringComparison.CurrentCultureIgnoreCase));

            if (result.ReviewDataSource == null)
            {
                _logger.LogInformation("Creating review data source for project {ProjectId}", projectId);
                
                result.ReviewDataSource = await _dataSourceService.CreateDataSourceAsync(projectId, new CreateDataSourceDto
                {
                    Name = "Review Stage Source",
                    Description = "Data source for assets in review stage",
                    SourceType = DataSourceType.MINIO_BUCKET
                });
            }
        }

        // Create or find completion data source
        result.CompletionDataSource = existingDataSources.Data.FirstOrDefault(ds =>
            ds.Name.Contains("completion", StringComparison.CurrentCultureIgnoreCase)
            || ds.Name.Contains("final", StringComparison.CurrentCultureIgnoreCase)
            || ds.Name.Contains("complete", StringComparison.CurrentCultureIgnoreCase));

        if (result.CompletionDataSource == null)
        {
            _logger.LogInformation("Creating completion data source for project {ProjectId}", projectId);
            
            result.CompletionDataSource = await _dataSourceService.CreateDataSourceAsync(projectId, new CreateDataSourceDto
            {
                Name = "Completion Stage Source",
                Description = "Data source for completed and exported assets",
                SourceType = DataSourceType.MINIO_BUCKET
            });
        }

        _logger.LogInformation("Data sources configured - Annotation: {AnnotationId}, Review: {ReviewId}, Completion: {CompletionId}",
            result.AnnotationDataSource?.Id, result.ReviewDataSource?.Id, result.CompletionDataSource?.Id);

        return result;
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
            ProjectId = workflow.ProjectId,
            StageCount = workflow.WorkflowStages?.Count ?? 0
        };
    }
}
