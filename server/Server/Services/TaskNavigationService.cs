using server.Models.DTOs.Task;
using server.Services.Interfaces;
using server.Repositories.Interfaces;
using System.Linq.Expressions;

namespace server.Services;

/// <summary>
/// Service for handling task navigation operations
/// </summary>
public class TaskNavigationService : ITaskNavigationService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ILogger<TaskNavigationService> _logger;

    public TaskNavigationService(ITaskRepository taskRepository, ILogger<TaskNavigationService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
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

        // Auto-assign next task if it's unassigned
        if (hasNext && nextTask != null && nextTask.AssignedToUserId == null)
        {
            _logger.LogInformation("Auto-assigning next task {NextTaskId} to user {UserId}", nextTask.TaskId, userId);
            
            try
            {
                nextTask.AssignedToUserId = userId;
                _taskRepository.Update(nextTask);
                await _taskRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-assign task {TaskId} to user {UserId} - task may have been assigned to another user", 
                    nextTask.TaskId, userId);
                
                // Re-fetch navigable tasks and try to find next available task
                var updatedNavigableTasks = await _taskRepository.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId);
                var nextAvailableTask = updatedNavigableTasks.FirstOrDefault(t => 
                    updatedNavigableTasks.IndexOf(t) > currentIndex && 
                    (t.AssignedToUserId == userId || t.AssignedToUserId == null));
                
                if (nextAvailableTask != null && nextAvailableTask.AssignedToUserId == null)
                {
                    try
                    {
                        nextAvailableTask.AssignedToUserId = userId;
                        _taskRepository.Update(nextAvailableTask);
                        await _taskRepository.SaveChangesAsync();
                        nextTask = nextAvailableTask;
                        _logger.LogInformation("Successfully auto-assigned alternative task {TaskId} to user {UserId}", 
                            nextAvailableTask.TaskId, userId);
                    }
                    catch
                    {
                        _logger.LogWarning("No more tasks available for assignment to user {UserId}", userId);
                        return new TaskNavigationDto
                        {
                            CurrentPosition = currentIndex + 1,
                            TotalTasks = navigableTasks.Count,
                            HasNext = false,
                            HasPrevious = currentIndex > 0,
                            Message = "No more tasks available for assignment"
                        };
                    }
                }
                else
                {
                    nextTask = nextAvailableTask;
                }
            }
        }

        var result = new TaskNavigationDto
        {
            TaskId = nextTask?.TaskId,
            AssetId = nextTask?.AssetId,
            HasNext = nextTaskHasNext,
            HasPrevious = nextIndex > 0,
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count,
            Message = hasNext ? null : "No more tasks available"
        };

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

        // Auto-assign previous task if it's unassigned
        if (hasPrevious && previousTask != null && previousTask.AssignedToUserId == null)
        {
            _logger.LogInformation("Auto-assigning previous task {PreviousTaskId} to user {UserId}", previousTask.TaskId, userId);
            
            try
            {
                previousTask.AssignedToUserId = userId;
                _taskRepository.Update(previousTask);
                await _taskRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-assign task {TaskId} to user {UserId} - task may have been assigned to another user", 
                    previousTask.TaskId, userId);
                
                // Re-fetch navigable tasks and try to find previous available task
                var updatedNavigableTasks = await _taskRepository.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId);
                var prevAvailableTask = updatedNavigableTasks.LastOrDefault(t => 
                    updatedNavigableTasks.IndexOf(t) < currentIndex && 
                    (t.AssignedToUserId == userId || t.AssignedToUserId == null));
                
                if (prevAvailableTask != null && prevAvailableTask.AssignedToUserId == null)
                {
                    try
                    {
                        prevAvailableTask.AssignedToUserId = userId;
                        _taskRepository.Update(prevAvailableTask);
                        await _taskRepository.SaveChangesAsync();
                        previousTask = prevAvailableTask;
                        _logger.LogInformation("Successfully auto-assigned alternative previous task {TaskId} to user {UserId}", 
                            prevAvailableTask.TaskId, userId);
                    }
                    catch
                    {
                        _logger.LogWarning("No more previous tasks available for assignment to user {UserId}", userId);
                        return new TaskNavigationDto
                        {
                            CurrentPosition = currentIndex + 1,
                            TotalTasks = navigableTasks.Count,
                            HasNext = currentIndex < navigableTasks.Count - 1,
                            HasPrevious = false,
                            Message = "No previous tasks available for assignment"
                        };
                    }
                }
                else
                {
                    previousTask = prevAvailableTask;
                }
            }
        }

        var result = new TaskNavigationDto
        {
            TaskId = previousTask?.TaskId,
            AssetId = previousTask?.AssetId,
            HasNext = previousIndex < navigableTasks.Count - 1,
            HasPrevious = previousTaskHasPrevious,
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count,
            Message = hasPrevious ? null : "No previous tasks available"
        };

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

        // Calculate task availability counts
        var taskCounts = await GetTaskAvailabilityCountsAsync(userId, currentTask.ProjectId, workflowStageId);

        var result = new TaskNavigationDto
        {
            TaskId = currentTaskId,
            AssetId = currentTask.AssetId,
            HasNext = currentIndex < navigableTasks.Count - 1,
            HasPrevious = currentIndex > 0,
            CurrentPosition = currentIndex + 1,
            TotalTasks = navigableTasks.Count,
            AvailableTasks = taskCounts.AvailableTasks,
            CompletedTasks = taskCounts.CompletedTasks,
            AssignedTasks = taskCounts.AssignedTasks
        };

        _logger.LogInformation("Navigation context: Position={Position}/{Total}, HasNext={HasNext}, HasPrevious={HasPrevious}, Available={Available}, Completed={Completed}, Assigned={Assigned}", 
            result.CurrentPosition, result.TotalTasks, result.HasNext, result.HasPrevious, result.AvailableTasks, result.CompletedTasks, result.AssignedTasks);

        return result;
    }
    
    /// <summary>
    /// Get task availability counts for a project and optional workflow stage
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="projectId">The project ID</param>
    /// <param name="workflowStageId">Optional workflow stage ID</param>
    /// <returns>Task availability counts</returns>
    private async Task<TaskAvailabilityCounts> GetTaskAvailabilityCountsAsync(string userId, int projectId, int? workflowStageId = null)
    {
        // Get all tasks for the project/stage (not just navigable ones)
        var filter = workflowStageId.HasValue 
            ? (Expression<Func<Models.Domain.Task, bool>>)(t => t.ProjectId == projectId && t.WorkflowStageId == workflowStageId.Value)
            : t => t.ProjectId == projectId;
            
        var allTasks = await _taskRepository.GetAllAsync(filter, pageSize: int.MaxValue);
        
        var availableTasks = 0;
        var completedTasks = 0; 
        var assignedTasks = 0;

        foreach (var task in allTasks)
        {
            // Skip archived and vetoed tasks from counts
            if (task.Status == Models.Domain.Enums.TaskStatus.ARCHIVED || 
                task.Status == Models.Domain.Enums.TaskStatus.VETOED)
                continue;

            if (task.Status == Models.Domain.Enums.TaskStatus.COMPLETED)
            {
                // Count completed tasks by this user
                if (task.AssignedToUserId == userId || task.LastWorkedOnByUserId == userId)
                    completedTasks++;
            }
            else if (string.IsNullOrEmpty(task.AssignedToUserId))
            {
                // Unassigned tasks are available
                availableTasks++;
            }
            else if (task.AssignedToUserId != userId)
            {
                // Tasks assigned to other users
                assignedTasks++;
            }
            else
            {
                // Tasks assigned to current user are available to them
                availableTasks++;
            }
        }

        return new TaskAvailabilityCounts
        {
            AvailableTasks = availableTasks,
            CompletedTasks = completedTasks,
            AssignedTasks = assignedTasks
        };
    }
    
    /// <summary>
    /// Helper class for task availability counts
    /// </summary>
    private class TaskAvailabilityCounts
    {
        public int AvailableTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int AssignedTasks { get; set; }
    }

}