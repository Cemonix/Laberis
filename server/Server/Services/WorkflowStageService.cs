using server.Models.Domain;
using server.Models.DTOs.WorkflowStage;
using server.Models.Common;
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
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
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

        var updatedStage = stage with
        {
            Name = updateDto.Name,
            Description = updateDto.Description,
            StageOrder = updateDto.StageOrder,
            StageType = updateDto.StageType,
            IsInitialStage = updateDto.IsInitialStage,
            IsFinalStage = updateDto.IsFinalStage,
            UiConfiguration = updateDto.UiConfiguration,
            InputDataSourceId = updateDto.InputDataSourceId,
            TargetDataSourceId = updateDto.TargetDataSourceId,
            UpdatedAt = DateTime.UtcNow
        };

        _workflowStageRepository.Update(updatedStage);
        await _workflowStageRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated workflow stage with ID: {StageId}", stageId);
        
        return MapToDto(updatedStage);
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
                    var updatedStage = stage with
                    {
                        StageOrder = newOrder,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _workflowStageRepository.Update(updatedStage);
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
}
