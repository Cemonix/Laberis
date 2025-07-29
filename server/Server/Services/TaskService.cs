using server.Models.Domain;
using server.Models.DTOs.Task;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskEventRepository _taskEventRepository;
    private readonly ITaskEventService _taskEventService;
    private readonly ITaskStatusValidator _taskStatusValidator;
    private readonly IAssetService _assetService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        ITaskEventRepository taskEventRepository,
        ITaskEventService taskEventService,
        ITaskStatusValidator taskStatusValidator,
        IAssetService assetService,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _taskEventRepository = taskEventRepository ?? throw new ArgumentNullException(nameof(taskEventRepository));
        _taskEventService = taskEventService ?? throw new ArgumentNullException(nameof(taskEventService));
        _taskStatusValidator = taskStatusValidator ?? throw new ArgumentNullException(nameof(taskStatusValidator));
        _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<TaskDto>> GetTasksForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching tasks for project: {ProjectId}", projectId);

        var (tasks, totalCount) = await _taskRepository.GetAllWithCountAsync(
            filter: t => t.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} tasks for project: {ProjectId}", tasks.Count(), projectId);

        var taskDtos = tasks.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<TaskDto>
        {
            Data = taskDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<PaginatedResponse<TaskDto>> GetTasksForUserAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching tasks for user: {UserId}", userId);

        var (tasks, totalCount) = await _taskRepository.GetAllWithCountAsync(
            filter: t => t.AssignedToUserId == userId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} tasks for user: {UserId}", tasks.Count(), userId);

        var taskDtos = tasks.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<TaskDto>
        {
            Data = taskDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<TaskDto?> GetTaskByIdAsync(int taskId)
    {
        _logger.LogInformation("Fetching task with ID: {TaskId}", taskId);

        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found.", taskId);
            return null;
        }

        _logger.LogInformation("Successfully fetched task with ID: {TaskId}", taskId);
        return MapToDto(task);
    }

    public async Task<PaginatedResponse<TaskDto>> GetTasksForWorkflowStageAsync(
        int projectId,
        int stageId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching tasks for workflow stage {StageId} in project {ProjectId}", stageId, projectId);

        var (tasks, totalCount) = await _taskRepository.GetAllWithCountAsync(
            filter: t => t.ProjectId == projectId && t.CurrentWorkflowStageId == stageId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} tasks for workflow stage {StageId} in project {ProjectId}", 
            tasks.Count(), stageId, projectId);

        var taskDtos = tasks.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<TaskDto>
        {
            Data = taskDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }


    public async Task<TaskDto> CreateTaskAsync(int projectId, CreateTaskDto createDto)
    {
        _logger.LogInformation("Creating new task for project: {ProjectId}, workflow: {WorkflowId}", projectId, createDto.WorkflowId);

        var task = new LaberisTask
        {
            Priority = createDto.Priority,
            AssetId = createDto.AssetId,
            ProjectId = projectId,
            WorkflowId = createDto.WorkflowId,
            AssignedToUserId = createDto.AssignedToUserId,
            CurrentWorkflowStageId = createDto.CurrentWorkflowStageId,
            DueDate = createDto.DueDate,
            Metadata = createDto.Metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created task with ID: {TaskId}", task.TaskId);

        return MapToDto(task);
    }

    public async Task<TaskDto?> UpdateTaskAsync(int taskId, UpdateTaskDto updateDto)
    {
        _logger.LogInformation("Updating task with ID: {TaskId}", taskId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for update.", taskId);
            return null;
        }

        // Update the task properties
        existingTask.Priority = updateDto.Priority;
        existingTask.AssignedToUserId = updateDto.AssignedToUserId ?? existingTask.AssignedToUserId;
        existingTask.CurrentWorkflowStageId = updateDto.CurrentWorkflowStageId ?? existingTask.CurrentWorkflowStageId;
        existingTask.DueDate = updateDto.DueDate ?? existingTask.DueDate;
        existingTask.Metadata = updateDto.Metadata ?? existingTask.Metadata;
        existingTask.CompletedAt = updateDto.CompletedAt ?? existingTask.CompletedAt;
        existingTask.ArchivedAt = updateDto.ArchivedAt ?? existingTask.ArchivedAt;
        existingTask.SuspendedAt = updateDto.SuspendedAt ?? existingTask.SuspendedAt;
        existingTask.DeferredAt = updateDto.DeferredAt ?? existingTask.DeferredAt;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated task with ID: {TaskId}", taskId);
        return MapToDto(existingTask);
    }

    public async Task<bool> DeleteTaskAsync(int taskId)
    {
        _logger.LogInformation("Deleting task with ID: {TaskId}", taskId);

        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for deletion.", taskId);
            return false;
        }

        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted task with ID: {TaskId}", taskId);
        return true;
    }

    public async Task<TaskDto?> AssignTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Assigning task {TaskId} to user: {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for assignment.", taskId);
            return null;
        }

        // Prevent assignment of suspended tasks
        if (existingTask.SuspendedAt.HasValue)
        {
            _logger.LogWarning("Attempted to assign suspended task {TaskId}. Suspended tasks cannot be assigned.", taskId);
            throw new InvalidOperationException($"Task {taskId} is suspended and cannot be assigned.");
        }

        // Prevent assignment of deferred tasks
        if (existingTask.DeferredAt.HasValue)
        {
            _logger.LogWarning("Attempted to assign deferred task {TaskId}. Deferred tasks cannot be assigned.", taskId);
            throw new InvalidOperationException($"Task {taskId} is deferred and cannot be assigned.");
        }

        existingTask.AssignedToUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create a task event for the assignment
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_ASSIGNED,
            Details = $"Task assigned to user {userId}",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully assigned task {TaskId} to user: {UserId}", taskId, userId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> MoveTaskToStageAsync(int taskId, int workflowStageId, string userId)
    {
        _logger.LogInformation("Moving task {TaskId} to workflow stage: {WorkflowStageId}", taskId, workflowStageId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for stage movement.", taskId);
            return null;
        }

        var previousStageId = existingTask.CurrentWorkflowStageId;

        existingTask.CurrentWorkflowStageId = workflowStageId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create a task event for the stage movement
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.STAGE_CHANGED,
            Details = $"Task moved from stage {previousStageId} to stage {workflowStageId}",
            TaskId = taskId,
            UserId = userId,
            FromWorkflowStageId = previousStageId,
            ToWorkflowStageId = workflowStageId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully moved task {TaskId} to workflow stage: {WorkflowStageId}", taskId, workflowStageId);
        return MapToDto(existingTask);
    }

    public async Task<int> CreateTasksForAllAssetsAsync(int projectId, int workflowId, int initialStageId)
    {
        _logger.LogInformation("Creating tasks for all assets in project {ProjectId} for workflow {WorkflowId}", projectId, workflowId);

        // Get all imported assets for the project that don't already have tasks
        var availableAssets = await _taskRepository.GetAvailableAssetsForTaskCreationAsync(projectId);

        if (!availableAssets.Any())
        {
            _logger.LogInformation("No available assets found for task creation in project {ProjectId}", projectId);
            return 0;
        }

        var tasksCreated = 0;
        foreach (var asset in availableAssets)
        {
            var createTaskDto = new CreateTaskDto
            {
                AssetId = asset.AssetId,
                WorkflowId = workflowId,
                CurrentWorkflowStageId = initialStageId,
                Priority = 1, // Default priority
                Metadata = null,
                AssignedToUserId = null
            };

            try
            {
                await CreateTaskAsync(projectId, createTaskDto);
                tasksCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create task for asset {AssetId} in project {ProjectId}", asset.AssetId, projectId);
            }
        }

        _logger.LogInformation("Successfully created {TasksCreated} tasks for project {ProjectId}", tasksCreated, projectId);
        return tasksCreated;
    }

    public async Task<int> CreateTasksForDataSourceAsync(int projectId, int workflowId, int workflowStageId, int dataSourceId)
    {
        _logger.LogInformation("Creating tasks for data source {DataSourceId} in project {ProjectId} for workflow stage {WorkflowStageId}", 
            dataSourceId, projectId, workflowStageId);

        // Get available assets from the specific data source
        var availableAssets = await _taskRepository.GetAvailableAssetsFromDataSourceAsync(projectId, dataSourceId);
        
        if (!availableAssets.Any())
        {
            _logger.LogInformation("No available assets found in data source {DataSourceId} for project {ProjectId}", 
                dataSourceId, projectId);
            return 0;
        }

        var tasksCreated = 0;
        foreach (var asset in availableAssets)
        {
            var createTaskDto = new CreateTaskDto
            {
                AssetId = asset.AssetId,
                WorkflowId = workflowId,
                CurrentWorkflowStageId = workflowStageId,
                Priority = 1, // Default priority
                Metadata = null,
                AssignedToUserId = null
            };

            try
            {
                await CreateTaskAsync(projectId, createTaskDto);
                tasksCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create task for asset {AssetId} in data source {DataSourceId}", 
                    asset.AssetId, dataSourceId);
            }
        }

        _logger.LogInformation("Successfully created {TasksCreated} tasks for data source {DataSourceId} in project {ProjectId}", 
            tasksCreated, dataSourceId, projectId);

        return tasksCreated;
    }

    public async Task<bool> HasAssetsAvailableAsync(int projectId, int dataSourceId)
    {
        _logger.LogInformation("Checking if data source {DataSourceId} has assets available for project {ProjectId}", dataSourceId, projectId);
        
        var assetsCount = await _taskRepository.GetAvailableAssetsCountAsync(projectId, dataSourceId);
        var hasAssets = assetsCount > 0;
        
        _logger.LogInformation("Data source {DataSourceId} has {AssetsCount} assets available", dataSourceId, assetsCount);
        return hasAssets;
    }

    public async Task<int> GetAvailableAssetsCountAsync(int projectId)
    {
        _logger.LogInformation("Getting available assets count for project {ProjectId}", projectId);
        
        var count = await _taskRepository.GetAvailableAssetsCountAsync(projectId);
        
        _logger.LogInformation("Project {ProjectId} has {AssetsCount} available assets", projectId, count);
        return count;
    }

    public async Task<TaskDto?> CompleteTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Completing task {TaskId} by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for completion", taskId);
            return null;
        }

        if (existingTask.CompletedAt != null)
        {
            _logger.LogWarning("Task {TaskId} is already completed", taskId);
            return MapToDto(existingTask);
        }

        // Mark task as completed and clear conflicting state timestamps
        existingTask.CompletedAt = DateTime.UtcNow;
        existingTask.SuspendedAt = null;  // Clear suspended state
        existingTask.DeferredAt = null;   // Clear deferred state
        // Note: Don't clear ArchivedAt here - completed tasks can still be archived later
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track completion
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_COMPLETED,
            Details = "Task completed",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully completed task {TaskId}", taskId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> CompleteAndMoveTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Completing and moving task {TaskId} by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for completion and move", taskId);
            return null;
        }

        if (existingTask.CompletedAt != null)
        {
            _logger.LogWarning("Task {TaskId} is already completed", taskId);
            return MapToDto(existingTask);
        }

        // Find next workflow stage
        var nextStage = await _taskRepository.GetNextWorkflowStageAsync(existingTask.CurrentWorkflowStageId);
        
        if (nextStage == null)
        {
            _logger.LogInformation("No next stage found for task {TaskId}, marking as completed only", taskId);
            return await CompleteTaskAsync(taskId, userId);
        }

        // Complete current task
        existingTask.CompletedAt = DateTime.UtcNow;
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent for completion
        var completionEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_COMPLETED,
            Details = $"Task completed and moved to next workflow stage",
            TaskId = taskId,
            UserId = userId,
            FromWorkflowStageId = existingTask.CurrentWorkflowStageId,
            ToWorkflowStageId = nextStage.WorkflowStageId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(completionEvent);
        await _taskEventRepository.SaveChangesAsync();

        // Create new task for next stage using data source-specific creation
        if (nextStage.InputDataSourceId.HasValue)
        {
            try
            {
                var tasksCreated = await CreateTasksForDataSourceAsync(
                    existingTask.ProjectId, 
                    existingTask.WorkflowId, 
                    nextStage.WorkflowStageId, 
                    nextStage.InputDataSourceId.Value);

                _logger.LogInformation("Created {TasksCreated} tasks for next stage {NextStageId}", 
                    tasksCreated, nextStage.WorkflowStageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tasks for next stage {NextStageId} after completing task {TaskId}", 
                    nextStage.WorkflowStageId, taskId);
                // Don't fail the completion if we can't create next stage tasks
            }
        }

        _logger.LogInformation("Successfully completed and moved task {TaskId} to next stage {NextStageId}", 
            taskId, nextStage.WorkflowStageId);

        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> MarkTaskIncompleteAsync(int taskId, string userId)
    {
        _logger.LogInformation("Marking task {TaskId} as incomplete by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for marking incomplete", taskId);
            return null;
        }

        if (existingTask.CompletedAt == null)
        {
            throw new InvalidOperationException($"Task {taskId} is not completed and cannot be marked as incomplete");
        }

        // Mark task as incomplete by clearing completion timestamp
        existingTask.CompletedAt = null;
        // Note: Don't clear SuspendedAt/DeferredAt here - task can be marked incomplete
        // while still being suspended/deferred (they're overlay states)
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track the action
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_REOPENED,
            Details = "Task marked as incomplete",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully marked task {TaskId} as incomplete", taskId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> SuspendTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Suspending task {TaskId} by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for suspension", taskId);
            return null;
        }

        if (existingTask.CompletedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is completed and cannot be suspended");
        }

        if (existingTask.ArchivedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is archived and cannot be suspended");
        }

        if (existingTask.SuspendedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is already suspended");
        }

        // Mark task as suspended and clear conflicting state timestamps
        existingTask.SuspendedAt = DateTime.UtcNow;
        existingTask.DeferredAt = null;  // Clear deferred state
        existingTask.CompletedAt = null; // Clear completed state  
        existingTask.ArchivedAt = null;  // Clear archived state
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track suspension
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_SUSPENDED,
            Details = "Task suspended",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully suspended task {TaskId}", taskId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> UnsuspendTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Unsuspending task {TaskId} by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for unsuspension", taskId);
            return null;
        }

        if (existingTask.SuspendedAt == null)
        {
            throw new InvalidOperationException($"Task {taskId} is not suspended and cannot be unsuspended");
        }

        // Clear the suspended status (task returns to its natural state based on assignment)
        existingTask.SuspendedAt = null;
        // Note: Don't clear other timestamps here - if task was completed before suspension,
        // unsuspending should NOT uncomplete it. Only clear suspended state.
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track unsuspension
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_REOPENED, // Using TASK_REOPENED as it's semantically similar
            Details = "Task unsuspended",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully unsuspended task {TaskId}", taskId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> DeferTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Deferring task {TaskId} by user {UserId}", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for deferring", taskId);
            return null;
        }

        if (existingTask.CompletedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is completed and cannot be deferred");
        }

        if (existingTask.ArchivedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is archived and cannot be deferred");
        }

        if (existingTask.SuspendedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is suspended and cannot be deferred");
        }

        // Mark task as deferred and clear conflicting state timestamps
        existingTask.DeferredAt = DateTime.UtcNow;
        existingTask.SuspendedAt = null;  // Clear suspended state
        existingTask.CompletedAt = null;  // Clear completed state
        existingTask.ArchivedAt = null;   // Clear archived state
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track deferral
        var taskEvent = new TaskEvent
        {
            EventType = TaskEventType.TASK_DEFERRED,
            Details = "Task deferred by user",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deferred task {TaskId}", taskId);
        return MapToDto(existingTask);
    }

    public async Task<TaskDto?> ChangeTaskStatusAsync(int taskId, server.Models.Domain.Enums.TaskStatus targetStatus, string userId, bool moveAsset = true)
    {
        _logger.LogInformation("Changing task {TaskId} status to {TargetStatus} by user {UserId} (moveAsset: {MoveAsset})", 
            taskId, targetStatus, userId, moveAsset);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for status change", taskId);
            return null;
        }

        var currentStatus = CalculateTaskStatus(existingTask, existingTask.CurrentWorkflowStage?.StageType?.ToString());
        
        // Skip if already in target status
        if (currentStatus == targetStatus)
        {
            _logger.LogInformation("Task {TaskId} is already in status {Status}", taskId, targetStatus);
            return MapToDto(existingTask);
        }

        // Validate the status transition
        var validationResult = _taskStatusValidator.ValidateStatusTransition(existingTask, currentStatus, targetStatus, userId);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(validationResult.ErrorMessage);
        }

        // Apply the status change
        ApplyStatusChange(existingTask, targetStatus, userId);

        // Handle asset movement and workflow progression if needed
        if (moveAsset)
        {
            var assetMovementResult = await _assetService.HandleTaskWorkflowAssetMovementAsync(existingTask, targetStatus, userId);
            
            // Apply archiving if the asset service determined it should happen
            if (assetMovementResult.ShouldArchiveTask && targetStatus == TaskStatus.COMPLETED)
            {
                existingTask.ArchivedAt = DateTime.UtcNow;
            }
            
            // Log any asset movement errors as warnings (don't fail the status change)
            if (!string.IsNullOrEmpty(assetMovementResult.ErrorMessage))
            {
                _logger.LogWarning("Asset movement warning for task {TaskId}: {ErrorMessage}", 
                    taskId, assetMovementResult.ErrorMessage);
            }
        }

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track the status change
        await _taskEventService.LogStatusChangeEventAsync(taskId, currentStatus, targetStatus, userId);

        _logger.LogInformation("Successfully changed task {TaskId} status from {CurrentStatus} to {TargetStatus}", 
            taskId, currentStatus, targetStatus);
        
        return MapToDto(existingTask);
    }


    private static void ApplyStatusChange(LaberisTask task, TaskStatus targetStatus, string userId)
    {
        var now = DateTime.UtcNow;
        
        // Clear all state timestamps first (mutual exclusion)
        task.SuspendedAt = null;
        task.DeferredAt = null;
        task.CompletedAt = null;
        task.ArchivedAt = null;
        
        // Set the appropriate timestamp for the target status
        switch (targetStatus)
        {
            case TaskStatus.SUSPENDED:
                task.SuspendedAt = now;
                break;
            case TaskStatus.DEFERRED:
                task.DeferredAt = now;
                break;
            case TaskStatus.COMPLETED:
                task.CompletedAt = now;
                break;
            case TaskStatus.ARCHIVED:
                task.ArchivedAt = now;
                task.CompletedAt = now; // Archived tasks must be completed
                break;
            // IN_PROGRESS, READY_FOR_* statuses are calculated based on assignment and no timestamps are set
        }
        
        task.LastWorkedOnByUserId = userId;
        task.UpdatedAt = now;
    }



    private static TaskStatus CalculateTaskStatus(LaberisTask task, string? stageType = null)
    {
        // With mutual exclusion, only one timestamp can be set at a time
        // Check in logical order of finality
        
        if (task.ArchivedAt.HasValue) return TaskStatus.ARCHIVED;
        if (task.SuspendedAt.HasValue) return TaskStatus.SUSPENDED;
        if (task.DeferredAt.HasValue) return TaskStatus.DEFERRED;
        if (task.CompletedAt.HasValue) return TaskStatus.COMPLETED;
        
        // Determine working state based on assignment
        if (!string.IsNullOrEmpty(task.AssignedToUserId))
        {
            return TaskStatus.IN_PROGRESS;
        }
        
        // Context-aware status for unstarted tasks based on workflow stage type
        if (!string.IsNullOrEmpty(stageType))
        {
            return stageType.ToUpperInvariant() switch
            {
                "ANNOTATION" => TaskStatus.READY_FOR_ANNOTATION,
                "REVISION" => TaskStatus.READY_FOR_REVIEW,
                "COMPLETION" => TaskStatus.READY_FOR_COMPLETION,
                _ => TaskStatus.NOT_STARTED
            };
        }
        
        return TaskStatus.NOT_STARTED;
    }

    private static TaskDto MapToDto(LaberisTask task)
    {
        return new TaskDto
        {
            Id = task.TaskId,
            Priority = task.Priority,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            ArchivedAt = task.ArchivedAt,
            SuspendedAt = task.SuspendedAt,
            DeferredAt = task.DeferredAt,
            Status = CalculateTaskStatus(task, task.CurrentWorkflowStage?.StageType?.ToString()),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            AssetId = task.AssetId,
            ProjectId = task.ProjectId,
            WorkflowId = task.WorkflowId,
            CurrentWorkflowStageId = task.CurrentWorkflowStageId,
            AssignedToEmail = task.AssignedToUser?.Email,
            LastWorkedOnByEmail = task.LastWorkedOnByUser?.Email
        };
    }
}
