using server.Models.DTOs.Task;
using server.Services.Interfaces;
using server.Repositories.Interfaces;

namespace server.Services;

/// <summary>
/// Service for handling task navigation operations
/// </summary>
public class TaskNavigationService : ITaskNavigationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskLockingService _taskLockingService;
    private readonly ILogger<TaskNavigationService> _logger;

    public TaskNavigationService(ITaskRepository taskRepository, ITaskLockingService taskLockingService, ILogger<TaskNavigationService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _taskLockingService = taskLockingService ?? throw new ArgumentNullException(nameof(taskLockingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TaskNavigationDto> GetNextTaskAsync(string userId, int currentTaskId, int? workflowStageId = null)
    {
        _logger.LogInformation("Getting next task for user {UserId} from current task {CurrentTaskId} in stage {WorkflowStageId}", 
            userId, currentTaskId, workflowStageId);

        var currentTask = await _taskRepository.GetByIdAsync(currentTaskId);
        if (currentTask == null)
        {
            _logger.LogWarning("Current task {CurrentTaskId} not found", currentTaskId);
            return new TaskNavigationDto();
        }

        // Get navigable tasks for the user
        var navigableTasks = await _taskRepository.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId);

        var currentIndex = navigableTasks.FindIndex(t => t.TaskId == currentTaskId);
        if (currentIndex == -1)
        {
            _logger.LogWarning("Current task {CurrentTaskId} not found in user's navigable tasks", currentTaskId);
            return new TaskNavigationDto 
            { 
                CurrentPosition = 0, 
                TotalTasks = navigableTasks.Count,
                Message = "Current task is not in your assigned task list"
            };
        }

        var nextIndex = currentIndex + 1;
        var hasNext = nextIndex < navigableTasks.Count;
        var nextTask = hasNext ? navigableTasks[nextIndex] : null;
        
        // Calculate if the next task has a task after it
        var nextTaskHasNext = hasNext && (nextIndex < navigableTasks.Count - 1);

        var result = new TaskNavigationDto
        {
            TaskId = nextTask?.TaskId,
            AssetId = nextTask?.AssetId,
            HasNext = nextTaskHasNext, // Use calculated value for the returned task
            HasPrevious = nextIndex > 0, // Use nextIndex instead of currentIndex
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count,
            Message = hasNext ? null : "No more tasks available"
        };

        // Attempt to lock the next task if one is found
        if (hasNext && nextTask != null)
        {
            _logger.LogInformation("Attempting to lock next task {TaskId} for user {UserId}", nextTask.TaskId, userId);
            var lockAcquired = await _taskLockingService.TryLockTaskAsync(nextTask.TaskId, userId);
            
            if (!lockAcquired)
            {
                _logger.LogWarning("Failed to acquire lock for task {TaskId} for user {UserId}", nextTask.TaskId, userId);
                // Note: We still return the navigation result even if locking fails
                // The client should handle this by checking if the task is locked when they try to work on it
            }
        }

        _logger.LogInformation("Next task navigation result: TaskId={TaskId}, HasNext={HasNext}, Position={Position}/{Total}", 
            result.TaskId, result.HasNext, result.CurrentPosition, result.TotalTasks);

        return result;
    }

    public async Task<TaskNavigationDto> GetPreviousTaskAsync(string userId, int currentTaskId, int? workflowStageId = null)
    {
        _logger.LogInformation("Getting previous task for user {UserId} from current task {CurrentTaskId} in stage {WorkflowStageId}", 
            userId, currentTaskId, workflowStageId);

        var currentTask = await _taskRepository.GetByIdAsync(currentTaskId);
        if (currentTask == null)
        {
            _logger.LogWarning("Current task {CurrentTaskId} not found", currentTaskId);
            return new TaskNavigationDto();
        }

        // Get navigable tasks for the user
        var navigableTasks = await _taskRepository.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId);

        var currentIndex = navigableTasks.FindIndex(t => t.TaskId == currentTaskId);
        if (currentIndex == -1)
        {
            _logger.LogWarning("Current task {CurrentTaskId} not found in user's navigable tasks", currentTaskId);
            return new TaskNavigationDto 
            { 
                CurrentPosition = 0, 
                TotalTasks = navigableTasks.Count,
                Message = "Current task is not in your assigned task list"
            };
        }

        var previousIndex = currentIndex - 1;
        var hasPrevious = previousIndex >= 0;
        var previousTask = hasPrevious ? navigableTasks[previousIndex] : null;
        
        // Calculate if the previous task has a task before it
        var previousTaskHasPrevious = previousIndex > 0;

        var result = new TaskNavigationDto
        {
            TaskId = previousTask?.TaskId,
            AssetId = previousTask?.AssetId,
            HasNext = previousIndex < navigableTasks.Count - 1, // Use previousIndex instead of currentIndex
            HasPrevious = previousTaskHasPrevious, // Use calculated value for the returned task
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count,
            Message = hasPrevious ? null : "No previous tasks available"
        };

        // Attempt to lock the previous task if one is found
        if (hasPrevious && previousTask != null)
        {
            _logger.LogInformation("Attempting to lock previous task {TaskId} for user {UserId}", previousTask.TaskId, userId);
            var lockAcquired = await _taskLockingService.TryLockTaskAsync(previousTask.TaskId, userId);
            
            if (!lockAcquired)
            {
                _logger.LogWarning("Failed to acquire lock for task {TaskId} for user {UserId}", previousTask.TaskId, userId);
                // Note: We still return the navigation result even if locking fails
                // The client should handle this by checking if the task is locked when they try to work on it
            }
        }

        _logger.LogInformation("Previous task navigation result: TaskId={TaskId}, HasPrevious={HasPrevious}, Position={Position}/{Total}", 
            result.TaskId, result.HasPrevious, result.CurrentPosition, result.TotalTasks);

        return result;
    }

    public async Task<TaskNavigationDto> GetNavigationContextAsync(string userId, int currentTaskId, int? workflowStageId = null)
    {
        _logger.LogInformation("Getting navigation context for user {UserId} and task {CurrentTaskId} in stage {WorkflowStageId}", 
            userId, currentTaskId, workflowStageId);

        var currentTask = await _taskRepository.GetByIdAsync(currentTaskId);
        if (currentTask == null)
        {
            _logger.LogWarning("Current task {CurrentTaskId} not found", currentTaskId);
            return new TaskNavigationDto();
        }

        // Get navigable tasks for the user
        var navigableTasks = await _taskRepository.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId);

        var currentIndex = navigableTasks.FindIndex(t => t.TaskId == currentTaskId);
        
        // If current task is not in navigable list (e.g., completed task being viewed), 
        // still provide context about available navigation
        if (currentIndex == -1)
        {
            _logger.LogInformation("Current task {CurrentTaskId} not in navigable list (likely completed/preview mode)", currentTaskId);
            return new TaskNavigationDto 
            { 
                CurrentPosition = 0, 
                TotalTasks = navigableTasks.Count,
                HasNext = navigableTasks.Count > 0,
                HasPrevious = false,
                Message = "Viewing task in preview mode"
            };
        }

        var result = new TaskNavigationDto
        {
            TaskId = currentTaskId,
            AssetId = currentTask.AssetId,
            HasNext = currentIndex < navigableTasks.Count - 1,
            HasPrevious = currentIndex > 0,
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count
        };

        _logger.LogInformation("Navigation context: Position={Position}/{Total}, HasNext={HasNext}, HasPrevious={HasPrevious}", 
            result.CurrentPosition, result.TotalTasks, result.HasNext, result.HasPrevious);

        return result;
    }

}