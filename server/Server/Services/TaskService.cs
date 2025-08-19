using server.Models.Domain;
using server.Models.DTOs.Task;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Exceptions;
using server.Utils;
using Microsoft.AspNetCore.Identity;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;
using server.Models.DTOs.TaskEvent;

namespace server.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
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
            Status = createDto.Status,
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
            string? newAssignedUserId;

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

        if (updateDto.Status.HasValue)
        {
            ApplyStatusChange(existingTask, updateDto.Status.Value);
        }

        existingTask.CurrentWorkflowStageId = updateDto.CurrentWorkflowStageId ?? existingTask.CurrentWorkflowStageId;
        existingTask.DueDate = updateDto.DueDate is not null ? Time.ConvertToUtcIfSpecified(updateDto.DueDate.Value) : existingTask.DueDate;
        existingTask.Metadata = updateDto.Metadata ?? existingTask.Metadata;
        existingTask.CompletedAt = updateDto.CompletedAt is not null ? Time.ConvertToUtcIfSpecified(updateDto.CompletedAt.Value) : existingTask.CompletedAt;
        existingTask.ArchivedAt = updateDto.ArchivedAt is not null ? Time.ConvertToUtcIfSpecified(updateDto.ArchivedAt.Value) : existingTask.ArchivedAt;
        existingTask.SuspendedAt = updateDto.SuspendedAt is not null ? Time.ConvertToUtcIfSpecified(updateDto.SuspendedAt.Value) : existingTask.SuspendedAt;
        existingTask.DeferredAt = updateDto.DeferredAt is not null ? Time.ConvertToUtcIfSpecified(updateDto.DeferredAt.Value) : existingTask.DeferredAt;

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

        // Prevent assignment of deferred tasks
        if (existingTask.Status == TaskStatus.DEFERRED)
        {
            _logger.LogWarning("Attempted to assign deferred task {TaskId}. Deferred tasks cannot be assigned.", taskId);
            throw new InvalidOperationException($"Task {taskId} is deferred and cannot be assigned.");
        }
        else if (existingTask.Status == TaskStatus.COMPLETED)
        {
            _logger.LogWarning("Attempted to assign completed task {TaskId}. Completed tasks cannot be assigned.", taskId);
            throw new InvalidOperationException($"Task {taskId} is completed and cannot be assigned.");
        }

        existingTask.AssignedToUserId = userId;
        existingTask.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        await _taskEventService.CreateTaskEventAsync(new CreateTaskEventDto
        {
            EventType = TaskEventType.TASK_ASSIGNED,
            Details = $"Task assigned to user {userId}",
            TaskId = taskId,
            UserId = assigningUserId
        });

        _logger.LogInformation("Successfully assigned task {TaskId} to user: {UserId}", taskId, userId);
        return MapToDto(existingTask);
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
                Status = GetInitialTaskStatusForStage(workflowStage.StageType),
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
                _logger.LogWarning(
                    ex, "Failed to create task for asset {AssetId} in workflow stage {WorkflowStageId}",
                    asset.AssetId, workflowStageId);
            }
        }

        _logger.LogInformation(
            "Successfully created {TasksCreated} tasks for workflow stage {WorkflowStageId} in project {ProjectId}",
            tasksCreated, workflowStageId, projectId);
        return tasksCreated;
    }

    public async Task<TaskDto?> ChangeTaskStatusAsync(int taskId, TaskStatus targetStatus, string userId)
    {
        _logger.LogInformation("Changing task {TaskId} status to {TargetStatus} by user {UserId}",
            taskId, targetStatus, userId);

        var existingTask = await _taskRepository.GetByIdAsync(taskId);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for status change", taskId);
            return null;
        }

        var currentStatus = existingTask.Status;

        // Skip if already in target status
        if (currentStatus == targetStatus)
        {
            _logger.LogInformation("Task {TaskId} is already in status {Status}", taskId, targetStatus);
            return MapToDto(existingTask);
        }

        // Validate the status transition
        var validationResult = _taskStatusValidator.ValidateStatusTransition(existingTask, currentStatus, targetStatus, userId);
        if (!validationResult)
        {
            throw new InvalidOperationException(
                $"Cannot change to {targetStatus} from {currentStatus}. {targetStatus} status changes are not allowed via direct status transitions."
            );
        }

        // Apply the status change
        ApplyStatusChange(existingTask, targetStatus);

        // Update LastWorkedOnByUserId only for statuses representing actual work
        if (targetStatus == TaskStatus.IN_PROGRESS || targetStatus == TaskStatus.COMPLETED)
        {
            existingTask.LastWorkedOnByUserId = userId;
        }

        await _taskRepository.SaveChangesAsync();

        // Create TaskEvent to track the status change
        await _taskEventService.CreateStatusChangeEventAsync(taskId, currentStatus, targetStatus, userId);

        _logger.LogInformation("Successfully changed task {TaskId} status from {CurrentStatus} to {TargetStatus}",
            taskId, currentStatus, targetStatus);

        return MapToDto(existingTask);
    }

    #region Private Methods
    
    /// <summary>
    /// Determines the initial task status based on the workflow stage type
    /// </summary>
    /// <param name="stageType">The workflow stage type</param>
    /// <returns>The appropriate initial task status</returns>
    private static TaskStatus GetInitialTaskStatusForStage(WorkflowStageType stageType)
    {
        return stageType switch
        {
            WorkflowStageType.ANNOTATION => TaskStatus.READY_FOR_ANNOTATION,
            WorkflowStageType.REVISION => TaskStatus.READY_FOR_REVIEW,
            WorkflowStageType.COMPLETION => TaskStatus.READY_FOR_COMPLETION,
            _ => TaskStatus.NOT_STARTED
        };
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

    private static void ApplyStatusChange(LaberisTask task, TaskStatus targetStatus)
    {
        var now = DateTime.UtcNow;

        task.Status = targetStatus;

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
                task.CompletedAt = now;
                break;
            case TaskStatus.VETOED:
                task.VetoedAt = now;
                break;
            case TaskStatus.CHANGES_REQUIRED:
                task.ChangesRequiredAt = now;
                break;
            default:
                break;
        }
        task.UpdatedAt = now;
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
            Status = task.Status,
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
    #endregion
}