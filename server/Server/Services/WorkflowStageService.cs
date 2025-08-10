using server.Models.Domain;
using server.Models.DTOs.WorkflowStage;
using server.Models.DTOs.ProjectMember;
using server.Models.Common;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class WorkflowStageService : IWorkflowStageService
{
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly ILogger<WorkflowStageService> _logger;

    public WorkflowStageService(
        IWorkflowStageRepository workflowStageRepository,
        ILogger<WorkflowStageService> logger)
    {
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<WorkflowStageDto>> GetWorkflowStagesAsync(
        int workflowId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    )
    {
        _logger.LogInformation("Fetching workflow stages for workflow: {WorkflowId}", workflowId);

        var (stages, totalCount) = await _workflowStageRepository.GetAllWithCountAsync(
            filter: ws => ws.WorkflowId == workflowId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "stage_order",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} workflow stages for workflow: {WorkflowId}", stages.Count(), workflowId);

        var stageDtos = stages.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<WorkflowStageDto>
        {
            Data = stageDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<WorkflowStageDto?> GetWorkflowStageByIdAsync(int stageId)
    {
        _logger.LogInformation("Fetching workflow stage with ID: {StageId}", stageId);
        
        var stage = await _workflowStageRepository.GetByIdAsync(stageId);
        
        if (stage == null)
        {
            _logger.LogWarning("Workflow stage with ID: {StageId} not found.", stageId);
            return null;
        }

        return MapToDto(stage);
    }

    public async Task<WorkflowStageDto> CreateWorkflowStageAsync(int workflowId, CreateWorkflowStageDto createDto)
    {
        _logger.LogInformation("Creating new workflow stage for workflow: {WorkflowId}", workflowId);

        // Validate data source exclusivity (unless this is a completion stage)
        await ValidateDataSourceExclusivityAsync(createDto.InputDataSourceId, createDto.StageType, workflowId, "input");
        await ValidateDataSourceExclusivityAsync(createDto.TargetDataSourceId, createDto.StageType, workflowId, "target");

        var stage = new WorkflowStage
        {
            Name = createDto.Name,
            Description = createDto.Description,
            StageOrder = createDto.StageOrder,
            StageType = createDto.StageType,
            IsInitialStage = createDto.IsInitialStage,
            IsFinalStage = createDto.IsFinalStage,
            UiConfiguration = createDto.UiConfiguration,
            WorkflowId = workflowId,
            InputDataSourceId = createDto.InputDataSourceId,
            TargetDataSourceId = createDto.TargetDataSourceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workflowStageRepository.AddAsync(stage);
        await _workflowStageRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created workflow stage with ID: {StageId}", stage.WorkflowStageId);
        
        return MapToDto(stage);
    }

    public async Task<WorkflowStageDto?> UpdateWorkflowStageAsync(int stageId, UpdateWorkflowStageDto updateDto)
    {
        _logger.LogInformation("Updating workflow stage with ID: {StageId}", stageId);

        var stage = await _workflowStageRepository.GetByIdAsync(stageId);
        
        if (stage == null)
        {
            _logger.LogWarning("Workflow stage with ID: {StageId} not found for update.", stageId);
            return null;
        }

        // Validate data source exclusivity if data sources are being changed
        var newStageType = updateDto.StageType ?? stage.StageType;
        var newInputDataSourceId = updateDto.InputDataSourceId ?? stage.InputDataSourceId;
        var newTargetDataSourceId = updateDto.TargetDataSourceId ?? stage.TargetDataSourceId;

        // Only validate if data sources are actually changing
        if (newInputDataSourceId != stage.InputDataSourceId)
        {
            await ValidateDataSourceExclusivityAsync(newInputDataSourceId, newStageType, stage.WorkflowId, "input");
        }
        if (newTargetDataSourceId != stage.TargetDataSourceId)
        {
            await ValidateDataSourceExclusivityAsync(newTargetDataSourceId, newStageType, stage.WorkflowId, "target");
        }

        stage.Name = updateDto.Name ?? stage.Name;
        stage.Description = updateDto.Description ?? stage.Description;
        stage.StageOrder = updateDto.StageOrder;
        stage.StageType = newStageType;
        stage.IsInitialStage = updateDto.IsInitialStage;
        stage.IsFinalStage = updateDto.IsFinalStage;
        stage.UiConfiguration = updateDto.UiConfiguration ?? stage.UiConfiguration;
        stage.InputDataSourceId = newInputDataSourceId;
        stage.TargetDataSourceId = newTargetDataSourceId;
        stage.UpdatedAt = DateTime.UtcNow;

        await _workflowStageRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated workflow stage with ID: {StageId}", stageId);
        
        return MapToDto(stage);
    }

    public async Task<bool> DeleteWorkflowStageAsync(int stageId)
    {
        _logger.LogInformation("Deleting workflow stage with ID: {StageId}", stageId);

        var stage = await _workflowStageRepository.GetByIdAsync(stageId);
        
        if (stage == null)
        {
            _logger.LogWarning("Workflow stage with ID: {StageId} not found for deletion.", stageId);
            return false;
        }

        _workflowStageRepository.Remove(stage);
        await _workflowStageRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted workflow stage with ID: {StageId}", stageId);
        
        return true;
    }

    public async Task<bool> ReorderWorkflowStagesAsync(int workflowId, Dictionary<int, int> stageOrderMap)
    {
        _logger.LogInformation("Reordering workflow stages for workflow: {WorkflowId}", workflowId);

        try
        {
            var stages = await _workflowStageRepository.FindAsync(ws => ws.WorkflowId == workflowId);
            
            foreach (var stage in stages)
            {
                if (stageOrderMap.TryGetValue(stage.WorkflowStageId, out var newOrder))
                {
                    stage.StageOrder = newOrder;
                    stage.UpdatedAt = DateTime.UtcNow;
                }
                
            }

            await _workflowStageRepository.SaveChangesAsync();
            
            _logger.LogInformation("Successfully reordered workflow stages for workflow: {WorkflowId}", workflowId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder workflow stages for workflow: {WorkflowId}", workflowId);
            return false;
        }
    }

    public async Task<IEnumerable<WorkflowStageDto>> GetWorkflowStagesWithConnectionsAsync(int workflowId)
    {
        _logger.LogInformation("Fetching workflow stages with connections for workflow: {WorkflowId}", workflowId);

        var stages = await _workflowStageRepository.GetWorkflowStagesWithConnectionsAsync(workflowId);
        
        _logger.LogInformation("Successfully fetched {Count} workflow stages with connections for workflow: {WorkflowId}", stages.Count(), workflowId);
        
        return stages.Select(MapToDtoWithConnections);
    }

    private static WorkflowStageDto MapToDto(WorkflowStage stage)
    {
        return new WorkflowStageDto
        {
            Id = stage.WorkflowStageId,
            Name = stage.Name,
            Description = stage.Description,
            StageOrder = stage.StageOrder,
            StageType = stage.StageType,
            IsInitialStage = stage.IsInitialStage,
            IsFinalStage = stage.IsFinalStage,
            CreatedAt = stage.CreatedAt,
            UpdatedAt = stage.UpdatedAt,
            WorkflowId = stage.WorkflowId,
            InputDataSourceId = stage.InputDataSourceId,
            TargetDataSourceId = stage.TargetDataSourceId
        };
    }

    private static WorkflowStageDto MapToDtoWithConnections(WorkflowStage stage)
    {
        return new WorkflowStageDto
        {
            Id = stage.WorkflowStageId,
            Name = stage.Name,
            Description = stage.Description,
            StageOrder = stage.StageOrder,
            StageType = stage.StageType,
            IsInitialStage = stage.IsInitialStage,
            IsFinalStage = stage.IsFinalStage,
            CreatedAt = stage.CreatedAt,
            UpdatedAt = stage.UpdatedAt,
            WorkflowId = stage.WorkflowId,
            InputDataSourceId = stage.InputDataSourceId,
            TargetDataSourceId = stage.TargetDataSourceId,
            IncomingConnections = stage.IncomingConnections?.Select(c => new WorkflowStageConnectionDto
            {
                Id = c.WorkflowStageConnectionId,
                FromStageId = c.FromStageId,
                ToStageId = c.ToStageId,
                Condition = c.Condition,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList() ?? [],
            OutgoingConnections = stage.OutgoingConnections?.Select(c => new WorkflowStageConnectionDto
            {
                Id = c.WorkflowStageConnectionId,
                FromStageId = c.FromStageId,
                ToStageId = c.ToStageId,
                Condition = c.Condition,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList() ?? [],
            Assignments = stage.StageAssignments?.Select(a => new WorkflowStageAssignmentDto
            {
                Id = a.WorkflowStageAssignmentId,
                WorkflowStageId = a.WorkflowStageId,
                ProjectMember = new ProjectMemberDto
                {
                    Id = a.ProjectMember.ProjectMemberId,
                    Role = a.ProjectMember.Role,
                    InvitedAt = a.ProjectMember.InvitedAt,
                    JoinedAt = a.ProjectMember.JoinedAt,
                    CreatedAt = a.ProjectMember.CreatedAt,
                    UpdatedAt = a.ProjectMember.UpdatedAt,
                    ProjectId = a.ProjectMember.ProjectId,
                    Email = a.ProjectMember.User?.Email ?? "",
                    UserName = a.ProjectMember.User?.UserName
                },
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            }).ToList() ?? []
        };
    }

    /// <summary>
    /// Validates that a data source is not already in use by other workflows (unless for completion stages).
    /// </summary>
    /// <param name="dataSourceId">The data source ID to validate.</param>
    /// <param name="stageType">The type of stage being created.</param>
    /// <param name="currentWorkflowId">The current workflow ID (to exclude from conflict check).</param>
    /// <param name="dataSourceType">Type description (input/target) for error messages.</param>
    private async System.Threading.Tasks.Task ValidateDataSourceExclusivityAsync(int? dataSourceId, WorkflowStageType? stageType, int currentWorkflowId, string dataSourceType)
    {
        if (!dataSourceId.HasValue) return;

        // Allow sharing for completion stages
        if (stageType == WorkflowStageType.COMPLETION)
        {
            _logger.LogInformation("Allowing data source {DataSourceId} sharing for completion stage", dataSourceId.Value);
            return;
        }

        var conflictingStages = await _workflowStageRepository.GetConflictingDataSourceUsageAsync(dataSourceId.Value, currentWorkflowId);
        
        if (conflictingStages.Any())
        {
            var conflictDescriptions = conflictingStages.Select(s => $"'{s.Name}' in workflow '{s.Workflow.Name}' (ID: {s.Workflow.WorkflowId})");
            var conflictList = string.Join(", ", conflictDescriptions);
            
            throw new InvalidOperationException(
                $"Data source {dataSourceId} cannot be used as {dataSourceType} data source because it is already in use by other workflow stages: {conflictList}. " +
                $"Each data source must be used by only one workflow stage to maintain data integrity. Consider creating a dedicated data source for this workflow stage.");
        }
    }

    public async Task<bool> ValidateStageBelongsToWorkflowAsync(int stageId, int workflowId)
    {
        _logger.LogInformation("Validating if stage {StageId} belongs to workflow {WorkflowId}", stageId, workflowId);

        var stage = await _workflowStageRepository.GetByIdAsync(stageId);
        if (stage == null)
        {
            _logger.LogWarning("Workflow stage with ID {StageId} not found", stageId);
            return false;
        }

        var isValid = stage.WorkflowId == workflowId;
        _logger.LogInformation("Stage {StageId} belongs to workflow {WorkflowId}: {IsValid}", stageId, workflowId, isValid);
        
        return isValid;
    }

    public async Task<IEnumerable<WorkflowStageDto>> GetDataSourceUsageConflictsAsync(int dataSourceId, int? excludeWorkflowId = null)
    {
        _logger.LogInformation("Getting data source usage conflicts for data source {DataSourceId}, excluding workflow {ExcludeWorkflowId}", 
            dataSourceId, excludeWorkflowId);

        var conflictingStages = await _workflowStageRepository.GetConflictingDataSourceUsageAsync(dataSourceId, excludeWorkflowId);
        var conflictDtos = conflictingStages.Select(MapToDto).ToList();

        _logger.LogInformation("Found {ConflictCount} conflicting stages for data source {DataSourceId}", 
            conflictDtos.Count, dataSourceId);

        return conflictDtos;
    }
}
