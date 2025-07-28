using server.Models.Domain;
using server.Models.DTOs.Task;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using LaberisTask = server.Models.Domain.Task;

namespace server.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskEventRepository _taskEventRepository;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        ITaskEventRepository taskEventRepository,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _taskEventRepository = taskEventRepository ?? throw new ArgumentNullException(nameof(taskEventRepository));
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

        // Mark task as completed
        existingTask.CompletedAt = DateTime.UtcNow;
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

        // Mark task as suspended
        existingTask.SuspendedAt = DateTime.UtcNow;
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

        // Clear the suspended status
        existingTask.SuspendedAt = null;
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
