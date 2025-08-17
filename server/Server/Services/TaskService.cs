using server.Models.Domain;
using server.Models.DTOs.Task;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Exceptions;
using Microsoft.AspNetCore.Identity;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskEventRepository _taskEventRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ITaskEventService _taskEventService;
    private readonly ITaskStatusValidator _taskStatusValidator;
    private readonly IAssetService _assetService;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IProjectMembershipService _projectMembershipService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepository,
        IAssetRepository assetRepository,
        ITaskEventRepository taskEventRepository,
        ITaskEventService taskEventService,
        ITaskStatusValidator taskStatusValidator,
        IAssetService assetService,
        IWorkflowStageRepository workflowStageRepository,
        UserManager<ApplicationUser> userManager,
        IProjectMembershipService projectMembershipService,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _taskEventRepository = taskEventRepository ?? throw new ArgumentNullException(nameof(taskEventRepository));
        _taskEventService = taskEventService ?? throw new ArgumentNullException(nameof(taskEventService));
        _taskStatusValidator = taskStatusValidator ?? throw new ArgumentNullException(nameof(taskStatusValidator));
        _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _projectMembershipService = projectMembershipService ?? throw new ArgumentNullException(nameof(projectMembershipService));
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

    public async Task<TaskDto?> GetTaskByIdWithAutoAssignAsync(int taskId, string userId)
    {
        _logger.LogInformation("Fetching task with ID: {TaskId} for user: {UserId} with auto-assignment", taskId, userId);

        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found.", taskId);
            return null;
        }

        // Auto-assign task if it's unassigned and in a valid state for assignment
        if (string.IsNullOrEmpty(task.AssignedToUserId) && CanAutoAssignTask(task))
        {
            try
            {
                _logger.LogInformation("Auto-assigning unassigned task {TaskId} to user {UserId}", taskId, userId);
                
                // Use existing AssignTaskAsync method to handle assignment logic
                var assignedTask = await AssignTaskAsync(taskId, userId);
                if (assignedTask != null)
                {
                    _logger.LogInformation("Successfully auto-assigned task {TaskId} to user {UserId}", taskId, userId);
                    return assignedTask;
                }
                else
                {
                    _logger.LogWarning("Failed to auto-assign task {TaskId} to user {UserId}, returning original task", taskId, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-assign task {TaskId} to user {UserId}, returning original task", taskId, userId);
                // Continue and return the original task even if auto-assignment fails
            }
        }

        _logger.LogInformation("Successfully fetched task with ID: {TaskId}", taskId);
        return MapToDto(task);
    }

    private static bool CanAutoAssignTask(LaberisTask task)
    {
        // Don't auto-assign tasks that are:
        // - Already completed
        // - Archived 
        // - Deferred (these require manual intervention)
        // - Suspended (these should be manually resumed first)
        return task.CompletedAt == null && 
               task.ArchivedAt == null && 
               task.DeferredAt == null && 
               task.SuspendedAt == null;
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

        // Reload the task with includes to ensure proper status calculation
        var createdTaskWithIncludes = await _taskRepository.GetByIdAsync(task.TaskId);
        if (createdTaskWithIncludes == null)
        {
            _logger.LogError("Failed to reload created task with ID: {TaskId}", task.TaskId);
            return MapToDto(task); // Fallback to original task
        }

        _logger.LogInformation("Successfully created task with ID: {TaskId}", task.TaskId);

        return MapToDto(createdTaskWithIncludes);
    }

    public async Task<TaskDto?> UpdateTaskAsync(int taskId, UpdateTaskDto updateDto, string? updatingUserId = null)
    {
        _logger.LogInformation("Updating task with ID: {TaskId} by user: {UpdatingUserId}", taskId, updatingUserId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for update.", taskId);
            return null;
        }

        // Update the task properties
        if (updateDto.Priority.HasValue)
        {
            existingTask.Priority = updateDto.Priority.Value;
        }
        
        // Handle assignment - support both userId and email assignment
        if (updateDto.AssignedToEmail != null)
        {
            string? newAssignedUserId = null;
            
            // Assignment by email - look up the user ID
            if (string.IsNullOrEmpty(updateDto.AssignedToEmail))
            {
                // Empty email means unassign
                newAssignedUserId = null;
            }
            else
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(updateDto.AssignedToEmail);
                if (user == null)
                {
                    _logger.LogWarning("User not found for assignment email: {Email}", updateDto.AssignedToEmail);
                    throw new NotFoundException($"User with email '{updateDto.AssignedToEmail}' not found");
                }
                newAssignedUserId = user.Id;
            }
            
            // Validate assignment permissions if assignment is changing and updatingUserId is provided
            if (existingTask.AssignedToUserId != newAssignedUserId && !string.IsNullOrEmpty(updatingUserId))
            {
                await ValidateTaskAssignmentPermissionAsync(existingTask.ProjectId, updatingUserId, newAssignedUserId);
            }
            
            existingTask.AssignedToUserId = newAssignedUserId;
        }
        else if (updateDto.AssignedToUserId != null && updateDto.AssignedToUserId != existingTask.AssignedToUserId)
        {
            // Validate assignment permissions if updatingUserId is provided
            if (!string.IsNullOrEmpty(updatingUserId))
            {
                await ValidateTaskAssignmentPermissionAsync(existingTask.ProjectId, updatingUserId, updateDto.AssignedToUserId);
            }
            
            // Assignment by userId (legacy support) - only update if explicitly provided
            existingTask.AssignedToUserId = updateDto.AssignedToUserId;
        }
        
        existingTask.CurrentWorkflowStageId = updateDto.CurrentWorkflowStageId ?? existingTask.CurrentWorkflowStageId;
        existingTask.DueDate = ConvertToUtcIfSpecified(updateDto.DueDate) ?? existingTask.DueDate;
        existingTask.Metadata = updateDto.Metadata ?? existingTask.Metadata;
        existingTask.CompletedAt = ConvertToUtcIfSpecified(updateDto.CompletedAt) ?? existingTask.CompletedAt;
        existingTask.ArchivedAt = ConvertToUtcIfSpecified(updateDto.ArchivedAt) ?? existingTask.ArchivedAt;
        existingTask.SuspendedAt = ConvertToUtcIfSpecified(updateDto.SuspendedAt) ?? existingTask.SuspendedAt;
        existingTask.DeferredAt = ConvertToUtcIfSpecified(updateDto.DeferredAt) ?? existingTask.DeferredAt;
        
        // Update working time if provided
        if (updateDto.WorkingTimeMs.HasValue)
        {
            existingTask.WorkingTimeMs = updateDto.WorkingTimeMs.Value;
        }
        
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

    public async Task<TaskDto?> AssignTaskAsync(int taskId, string userId, string? assigningUserId = null)
    {
        _logger.LogInformation("Assigning task {TaskId} to user: {UserId} by user: {AssigningUserId}", taskId, userId, assigningUserId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID: {TaskId} not found for assignment.", taskId);
            return null;
        }

        // Validate assignment permissions if assigningUserId is provided
        if (!string.IsNullOrEmpty(assigningUserId))
        {
            await ValidateTaskAssignmentPermissionAsync(existingTask.ProjectId, assigningUserId, userId);
        }

        // Auto-resume suspended tasks when assigning them
        if (existingTask.SuspendedAt.HasValue)
        {
            _logger.LogInformation("Task {TaskId} is suspended, auto-resuming before assignment", taskId);
            existingTask.SuspendedAt = null; // Clear suspended state
            existingTask.UpdatedAt = DateTime.UtcNow;
            // Note: We intentionally preserve other state timestamps like ChangesRequiredAt, VetoedAt, etc.
            // when auto-resuming from suspension, as suspension is a temporary overlay state
            
            // Create TaskEvent for auto-resume
            var resumeEvent = new TaskEvent
            {
                EventType = TaskEventType.STATUS_CHANGED,
                Details = "Task automatically resumed during assignment",
                TaskId = taskId,
                UserId = assigningUserId ?? userId,
                CreatedAt = DateTime.UtcNow
            };
            await _taskEventRepository.AddAsync(resumeEvent);
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
            UserId = assigningUserId ?? userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully assigned task {TaskId} to user: {UserId}", taskId, userId);
        return MapToDto(existingTask);
    }

    /// <summary>
    /// Validates that the assigning user has permission to assign tasks to the target user.
    /// REVIEWER role users can only assign tasks to themselves or unassign tasks.
    /// MANAGER role users can assign tasks to any project member.
    /// </summary>
    /// <param name="projectId">The project ID for role validation</param>
    /// <param name="assigningUserId">The user performing the assignment</param>
    /// <param name="targetUserId">The user being assigned the task (null for unassignment)</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the assignment is not permitted</exception>
    private async System.Threading.Tasks.Task ValidateTaskAssignmentPermissionAsync(int projectId, string assigningUserId, string? targetUserId)
    {
        var assigningUserMembership = await _projectMembershipService.GetProjectMembershipAsync(assigningUserId, projectId);
        if (assigningUserMembership == null)
        {
            _logger.LogWarning("Assigning user {AssigningUserId} is not a member of project {ProjectId}", assigningUserId, projectId);
            throw new UnauthorizedAccessException("User is not a member of this project");
        }

        // MANAGER role can assign tasks to anyone or unassign tasks
        if (assigningUserMembership.Role == ProjectRole.MANAGER)
        {
            _logger.LogDebug("MANAGER user {AssigningUserId} can assign/unassign tasks", assigningUserId);
            return;
        }

        // REVIEWER role can only assign tasks to themselves or unassign tasks
        if (assigningUserMembership.Role == ProjectRole.REVIEWER)
        {
            // Allow unassigning tasks (targetUserId is null or empty)
            if (string.IsNullOrEmpty(targetUserId))
            {
                _logger.LogDebug("REVIEWER user {AssigningUserId} is unassigning task", assigningUserId);
                return;
            }

            // Allow assigning tasks to themselves only
            if (assigningUserId != targetUserId)
            {
                _logger.LogWarning("REVIEWER user {AssigningUserId} attempted to assign task to different user {TargetUserId}", assigningUserId, targetUserId);
                throw new UnauthorizedAccessException("Reviewers can only assign tasks to themselves");
            }
            _logger.LogDebug("REVIEWER user {AssigningUserId} is assigning task to themselves", assigningUserId);
            return;
        }

        // ANNOTATOR and VIEWER roles should not have task:assign permission, but check anyway
        _logger.LogWarning("User {AssigningUserId} with role {Role} attempted to assign task", assigningUserId, assigningUserMembership.Role);
        throw new UnauthorizedAccessException($"Users with role {assigningUserMembership.Role} cannot assign tasks");
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

        // Get all imported assets for the project that don't already have tasks, filtered by the initial stage's input data source
        var availableAssets = await _assetRepository.GetAvailableAssetsForTaskCreationAsync(projectId, initialStageId);

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

    public async Task<int> CreateTasksForWorkflowStageAsync(int projectId, int workflowId, int workflowStageId)
    {
        _logger.LogInformation("Creating tasks for workflow stage {WorkflowStageId} in project {ProjectId}", workflowStageId, projectId);

        // Get the workflow stage to access its type for proper status calculation
        var workflowStage = await _workflowStageRepository.GetByIdAsync(workflowStageId);
        if (workflowStage == null)
        {
            _logger.LogError("Workflow stage {WorkflowStageId} not found", workflowStageId);
            return 0;
        }

        // Get assets specifically from the workflow stage's input data source
        var availableAssets = await _assetRepository.GetAvailableAssetsForTaskCreationAsync(projectId, workflowStageId);

        if (!availableAssets.Any())
        {
            _logger.LogInformation("No available assets found for workflow stage {WorkflowStageId} in project {ProjectId}", workflowStageId, projectId);
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
                var createdTask = await CreateTaskAsync(projectId, createTaskDto);
                _logger.LogDebug("Created task {TaskId} with status {Status} for asset {AssetId} in {StageType} stage", 
                    createdTask.Id, createdTask.Status, asset.AssetId, workflowStage.StageType);
                tasksCreated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create task for asset {AssetId} in workflow stage {WorkflowStageId}", asset.AssetId, workflowStageId);
            }
        }

        _logger.LogInformation("Successfully created {TasksCreated} tasks for workflow stage {WorkflowStageId} in project {ProjectId}", tasksCreated, workflowStageId, projectId);
        return tasksCreated;
    }

    public async Task<int> CreateTasksForDataSourceAsync(int projectId, int workflowId, int workflowStageId, int dataSourceId)
    {
        _logger.LogInformation("Creating tasks for data source {DataSourceId} in project {ProjectId} for workflow stage {WorkflowStageId}", 
            dataSourceId, projectId, workflowStageId);

        // Get available assets from the specific data source
        var availableAssets = await _assetRepository.GetAvailableAssetsFromDataSourceAsync(projectId, dataSourceId);
        
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
            EventType = TaskEventType.STATUS_CHANGED,
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

        // Check if there are any existing tasks for this asset to determine completion behavior
        var existingTasksForAsset = await GetTasksForAssetAsync(existingTask.AssetId, existingTask.WorkflowId);
        var completedTasksForAsset = existingTasksForAsset.Where(t => t.CompletedAt.HasValue && t.TaskId != taskId).ToList();
        
        if (completedTasksForAsset.Count != 0)
        {
            // Case 2 & 3: There is already a completed task for this asset
            var vetoedTask = completedTasksForAsset.FirstOrDefault(t => t.VetoedAt.HasValue);
            
            if (vetoedTask != null)
            {
                // Case 2: There is a vetoed task - reactivate it
                _logger.LogInformation("Task {TaskId} completion: Found vetoed task {VetoedTaskId} for asset {AssetId}, reactivating", 
                    taskId, vetoedTask.TaskId, existingTask.AssetId);
                return await CompleteChangesRequiredTaskAsync(taskId, userId);
            }
            else
            {
                // Case 3: There is a completed task but it's not vetoed - data integrity violation
                var completedTaskId = completedTasksForAsset.First().TaskId;
                _logger.LogError("Data integrity violation: Asset {AssetId} already has a completed task {CompletedTaskId} that is not vetoed. Cannot complete task {TaskId}.",
                    existingTask.AssetId, completedTaskId, taskId);
                throw new InvalidOperationException("Data integrity violation");
            }
        }
        
        // Case 1: No existing completed tasks for this asset - proceed with normal completion

        // Find next workflow stage
        var nextStage = await _workflowStageRepository.GetNextWorkflowStageAsync(existingTask.CurrentWorkflowStageId);
        
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
            EventType = TaskEventType.STATUS_CHANGED,
            Details = $"Task completed and moved to next workflow stage",
            TaskId = taskId,
            UserId = userId,
            FromWorkflowStageId = existingTask.CurrentWorkflowStageId,
            ToWorkflowStageId = nextStage.WorkflowStageId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(completionEvent);
        await _taskEventRepository.SaveChangesAsync();

        // Handle asset movement to next stage and create tasks
        if (nextStage.InputDataSourceId.HasValue)
        {
            try
            {
                // Use the same asset movement logic as ChangeTaskStatusAsync for consistency
                var assetMovementResult = await _assetService.HandleTaskWorkflowAssetMovementAsync(existingTask, TaskStatus.COMPLETED, userId);
                
                if (assetMovementResult.AssetMoved && assetMovementResult.TargetWorkflowStageId.HasValue)
                {
                    // Create tasks for the next workflow stage
                    var tasksCreated = await CreateTasksForWorkflowStageAsync(
                        existingTask.ProjectId, 
                        existingTask.WorkflowId, 
                        assetMovementResult.TargetWorkflowStageId.Value);

                    _logger.LogInformation("Moved asset and created {TasksCreated} tasks for next stage {NextStageId}", 
                        tasksCreated, nextStage.WorkflowStageId);
                }
                else if (!string.IsNullOrEmpty(assetMovementResult.ErrorMessage))
                {
                    _logger.LogWarning("Asset movement failed for task {TaskId}: {ErrorMessage}", 
                        taskId, assetMovementResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move asset and create tasks for next stage {NextStageId} after completing task {TaskId}", 
                    nextStage.WorkflowStageId, taskId);
                // Don't fail the completion if we can't create next stage tasks
            }
        }

        _logger.LogInformation("Successfully completed and moved task {TaskId} to next stage {NextStageId}", 
            taskId, nextStage.WorkflowStageId);

        return MapToDto(existingTask);
    }

    private async Task<TaskDto?> CompleteChangesRequiredTaskAsync(int taskId, string userId)
    {
        _logger.LogInformation("Completing CHANGES_REQUIRED task {TaskId} and reactivating vetoed task by user {UserId}", taskId, userId);

        var changesRequiredTask = await _taskRepository.GetByIdAsync(taskId);
        if (changesRequiredTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for changes required completion", taskId);
            return null;
        }

        // Complete the annotation task with changes required
        changesRequiredTask.CompletedAt = DateTime.UtcNow;
        changesRequiredTask.ChangesRequiredAt = null;  // Clear the changes required flag
        // Clear any conflicting state timestamps to ensure proper completion
        changesRequiredTask.SuspendedAt = null;
        changesRequiredTask.DeferredAt = null;
        changesRequiredTask.LastWorkedOnByUserId = userId;
        changesRequiredTask.UpdatedAt = DateTime.UtcNow;

        // Find the vetoed task for the same asset in review or completion stages
        var vetoedTasks = await _taskRepository.FindAsync(t => 
            t.AssetId == changesRequiredTask.AssetId && 
            t.WorkflowId == changesRequiredTask.WorkflowId &&
            t.VetoedAt.HasValue &&
            t.CurrentWorkflowStage != null &&
            (t.CurrentWorkflowStage.StageType == WorkflowStageType.REVISION || 
             t.CurrentWorkflowStage.StageType == WorkflowStageType.COMPLETION));

        var vetoedTask = vetoedTasks.FirstOrDefault();
        
        if (vetoedTask != null)
        {
            // Reactivate the vetoed task by clearing the veto timestamp
            vetoedTask.VetoedAt = null;
            vetoedTask.UpdatedAt = DateTime.UtcNow;
            
            _logger.LogInformation("Reactivated vetoed task {VetoedTaskId} for asset {AssetId} in stage {StageId}", 
                vetoedTask.TaskId, changesRequiredTask.AssetId, vetoedTask.CurrentWorkflowStageId);

            // Create TaskEvent for reactivation
            var reactivationEvent = new TaskEvent
            {
                EventType = TaskEventType.STATUS_CHANGED,
                Details = $"Task reactivated after changes were completed in annotation",
                TaskId = vetoedTask.TaskId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _taskEventRepository.AddAsync(reactivationEvent);
        }
        else
        {
            _logger.LogWarning("No vetoed task found for asset {AssetId} in workflow {WorkflowId}", 
                changesRequiredTask.AssetId, changesRequiredTask.WorkflowId);
        }

        // Create TaskEvent for completion of changes required task
        var completionEvent = new TaskEvent
        {
            EventType = TaskEventType.STATUS_CHANGED,
            Details = $"CHANGES_REQUIRED task completed, changes addressed",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(completionEvent);
        await _taskRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully completed CHANGES_REQUIRED task {TaskId} and reactivated vetoed task", taskId);

        return MapToDto(changesRequiredTask);
    }

    /// <summary>
    /// Gets all tasks for a specific asset within a workflow to enable asset-centric task management
    /// </summary>
    private async Task<List<LaberisTask>> GetTasksForAssetAsync(int assetId, int workflowId)
    {
        var tasks = await _taskRepository.FindAsync(t => 
            t.AssetId == assetId && 
            t.WorkflowId == workflowId);
        
        return [.. tasks];
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
            EventType = TaskEventType.STATUS_CHANGED,
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
            EventType = TaskEventType.STATUS_CHANGED,
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
            EventType = TaskEventType.STATUS_CHANGED,
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

    public async Task<TaskDto?> ReturnTaskForReworkAsync(int taskId, string userId, string? reason = null)
    {
        _logger.LogInformation("Returning task {TaskId} for rework by user {UserId} (reviewer or manager)", taskId, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for return", taskId);
            return null;
        }

        // Allow returning tasks in review stages (by reviewers) or completion stages (by managers)
        var currentStageType = existingTask.CurrentWorkflowStage?.StageType;
        _logger.LogInformation("Task {TaskId} workflow stage info - StageId: {StageId}, StageType: {StageType}, StageName: {StageName}", 
            taskId, 
            existingTask.CurrentWorkflowStageId, 
            currentStageType,
            existingTask.CurrentWorkflowStage?.Name);
        
        if (currentStageType != WorkflowStageType.REVISION && currentStageType != WorkflowStageType.COMPLETION)
        {
            throw new InvalidOperationException($"Task {taskId} is not in a review or completion stage (current: {currentStageType}) and cannot be returned for rework");
        }

        if (existingTask.ArchivedAt != null)
        {
            throw new InvalidOperationException($"Task {taskId} is archived and cannot be returned");
        }

        // Set the current review/completion task status to VETOED
        existingTask.VetoedAt = DateTime.UtcNow;
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        // Create TaskEvent for the veto action
        var vetoEvent = new TaskEvent
        {
            EventType = TaskEventType.STATUS_CHANGED,
            Details = $"Task vetoed{(string.IsNullOrEmpty(reason) ? "" : $": {reason}")}",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(vetoEvent);

        // Find the existing annotation task for the same asset
        var annotationTasks = await _taskRepository.FindAsync(t => 
            t.AssetId == existingTask.AssetId && 
            t.WorkflowId == existingTask.WorkflowId &&
            t.CurrentWorkflowStage != null &&
            t.CurrentWorkflowStage.StageType == WorkflowStageType.ANNOTATION);

        var annotationTask = annotationTasks.FirstOrDefault();
        
        if (annotationTask != null)
        {
            // Update existing annotation task to CHANGES_REQUIRED status
            annotationTask.ChangesRequiredAt = DateTime.UtcNow;
            annotationTask.SuspendedAt = null;
            annotationTask.DeferredAt = null;
            annotationTask.CompletedAt = null;
            annotationTask.ArchivedAt = null;
            annotationTask.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updated existing annotation task {AnnotationTaskId} to CHANGES_REQUIRED status for asset {AssetId}", 
                annotationTask.TaskId, existingTask.AssetId);

            // Create TaskEvent for the changes required action
            var changesRequiredEvent = new TaskEvent
            {
                EventType = TaskEventType.STATUS_CHANGED,
                Details = $"Task requires changes due to veto{(string.IsNullOrEmpty(reason) ? "" : $": {reason}")}",
                TaskId = annotationTask.TaskId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _taskEventRepository.AddAsync(changesRequiredEvent);
        }
        else
        {
            _logger.LogWarning("No existing annotation task found for asset {AssetId} in workflow {WorkflowId} - creating new annotation task", 
                existingTask.AssetId, existingTask.WorkflowId);
            
            // Find the annotation stage for this workflow
            var annotationStages = await _workflowStageRepository.FindAsync(ws => 
                ws.WorkflowId == existingTask.WorkflowId &&
                ws.StageType == WorkflowStageType.ANNOTATION);
            
            var annotationStage = annotationStages.FirstOrDefault();
            
            if (annotationStage != null)
            {
                // Create new annotation task for the asset since it was never in annotation stage
                var newTask = new Models.Domain.Task
                {
                    AssetId = existingTask.AssetId,
                    WorkflowId = existingTask.WorkflowId,
                    CurrentWorkflowStageId = annotationStage.WorkflowStageId,
                    ProjectId = existingTask.ProjectId,
                    Priority = existingTask.Priority,
                    ChangesRequiredAt = DateTime.UtcNow, // Set to CHANGES_REQUIRED immediately
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Metadata = null,
                    AssignedToUserId = null
                };

                await _taskRepository.AddAsync(newTask);
                await _taskRepository.SaveChangesAsync(); // Save to get the auto-generated TaskId
                
                _logger.LogInformation("Created new annotation task {NewTaskId} for asset {AssetId} that was vetoed from {StageType} stage", 
                    newTask.TaskId, existingTask.AssetId, existingTask.CurrentWorkflowStage?.StageType);

                // Create TaskEvent for the new task creation (now that we have a valid TaskId)
                var taskCreationEvent = new TaskEvent
                {
                    EventType = TaskEventType.STATUS_CHANGED,
                    Details = $"New annotation task created due to veto from {existingTask.CurrentWorkflowStage?.StageType} stage{(string.IsNullOrEmpty(reason) ? "" : $": {reason}")}",
                    TaskId = newTask.TaskId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _taskEventRepository.AddAsync(taskCreationEvent);
            }
            else
            {
                _logger.LogError("No annotation stage found for workflow {WorkflowId} - cannot create veto task", existingTask.WorkflowId);
            }
        }

        // Handle asset movement back to annotation stage
        try
        {
            var assetMovementResult = await _assetService.HandleTaskVetoAssetMovementAsync(
                existingTask, userId);
            
            if (!assetMovementResult.AssetMoved && !string.IsNullOrEmpty(assetMovementResult.ErrorMessage))
            {
                _logger.LogWarning("Asset movement failed when returning task {TaskId} for rework: {ErrorMessage}", 
                    taskId, assetMovementResult.ErrorMessage);
            }
            else if (assetMovementResult.AssetMoved)
            {
                _logger.LogInformation("Successfully moved asset {AssetId} back to annotation stage for task {TaskId}", 
                    existingTask.AssetId, taskId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move asset back to annotation when returning task {TaskId} for rework", taskId);
            // Don't fail the return operation if asset movement fails
        }

        await _taskRepository.SaveChangesAsync();
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully returned task {TaskId} for rework", taskId);
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
            EventType = TaskEventType.STATUS_CHANGED,
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
            
            // Create tasks for the next workflow stage if asset was successfully moved
            if (assetMovementResult.AssetMoved && assetMovementResult.TargetWorkflowStageId.HasValue)
            {
                try
                {
                    var tasksCreated = await CreateTasksForWorkflowStageAsync(
                        existingTask.ProjectId, 
                        existingTask.WorkflowId, 
                        assetMovementResult.TargetWorkflowStageId.Value);
                    
                    _logger.LogInformation("Created {TasksCreated} new tasks for workflow stage {TargetStageId} after moving asset for completed task {TaskId}", 
                        tasksCreated, assetMovementResult.TargetWorkflowStageId.Value, taskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create tasks for next workflow stage {TargetStageId} after asset movement for task {TaskId}", 
                        assetMovementResult.TargetWorkflowStageId.Value, taskId);
                    // Don't fail the status change if task creation fails - log as warning
                }
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
        task.VetoedAt = null;
        task.ChangesRequiredAt = null;
        
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
            case TaskStatus.VETOED:
                task.VetoedAt = now;
                break;
            case TaskStatus.CHANGES_REQUIRED:
                task.ChangesRequiredAt = now;
                break;
            // IN_PROGRESS, READY_FOR_* statuses are calculated based on assignment and no timestamps are set
        }
        
        // Update LastWorkedOnByUserId only for statuses representing actual work  
        if (targetStatus == TaskStatus.IN_PROGRESS || targetStatus == TaskStatus.COMPLETED)  
        {  
            task.LastWorkedOnByUserId = userId;  
        }  
        task.UpdatedAt = now;
    }



    private static TaskStatus CalculateTaskStatus(LaberisTask task, string? stageType = null)
    {
        // With mutual exclusion, only one timestamp can be set at a time
        // Check in logical order of finality
        
        if (task.ArchivedAt.HasValue) return TaskStatus.ARCHIVED;
        if (task.VetoedAt.HasValue) return TaskStatus.VETOED;
        if (task.SuspendedAt.HasValue) return TaskStatus.SUSPENDED;
        if (task.DeferredAt.HasValue) return TaskStatus.DEFERRED;
        if (task.ChangesRequiredAt.HasValue) return TaskStatus.CHANGES_REQUIRED;
        if (task.CompletedAt.HasValue) return TaskStatus.COMPLETED;
        
        // Check if task has been worked on (has LastWorkedOnByUserId set)
        // Only tasks that have actual work done should be IN_PROGRESS
        if (!string.IsNullOrEmpty(task.AssignedToUserId) && !string.IsNullOrEmpty(task.LastWorkedOnByUserId))
        {
            return TaskStatus.IN_PROGRESS;
        }
        
        // Context-aware status for unstarted tasks based on workflow stage type
        // This applies to both unassigned tasks and newly assigned tasks that haven't been worked on
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
            VetoedAt = task.VetoedAt,
            ChangesRequiredAt = task.ChangesRequiredAt,
            WorkingTimeMs = task.WorkingTimeMs,
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

    /// <summary>
    /// Converts DateTime to UTC if it has Kind=Unspecified, otherwise returns as-is.
    /// This handles the PostgreSQL timezone requirement.
    /// </summary>
    private static DateTime? ConvertToUtcIfSpecified(DateTime? dateTime)
    {
        if (dateTime == null) return null;
        
        // If Kind is Unspecified, treat it as UTC (common for date-only inputs)
        if (dateTime.Value.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime.Value, DateTimeKind.Utc);
        }
        
        // If it's already UTC, return as-is
        if (dateTime.Value.Kind == DateTimeKind.Utc)
        {
            return dateTime.Value;
        }
        
        // If it's Local, convert to UTC
        return dateTime.Value.ToUniversalTime();
    }
}
