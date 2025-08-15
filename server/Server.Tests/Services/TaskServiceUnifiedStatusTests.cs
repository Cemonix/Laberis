using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Moq;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.TaskEvent;
using server.Repositories.Interfaces;
using server.Services;
using server.Services.Interfaces;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace Server.Tests.Services;

public class TaskServiceUnifiedStatusTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ITaskEventRepository> _mockTaskEventRepository;
    private readonly Mock<ITaskEventService> _mockTaskEventService;
    private readonly Mock<ITaskStatusValidator> _mockTaskStatusValidator;
    private readonly Mock<IAssetService> _mockAssetService;
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IProjectMembershipService> _mockProjectMembershipService;
    private readonly Mock<ILogger<TaskService>> _mockLogger;
    private readonly TaskService _taskService;

    public TaskServiceUnifiedStatusTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockTaskEventRepository = new Mock<ITaskEventRepository>();
        _mockTaskEventService = new Mock<ITaskEventService>();
        _mockTaskStatusValidator = new Mock<ITaskStatusValidator>();
        _mockAssetService = new Mock<IAssetService>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockUserManager = MockUserManager();
        _mockProjectMembershipService = new Mock<IProjectMembershipService>();
        _mockLogger = new Mock<ILogger<TaskService>>();

        // Setup default asset service behavior
        _mockAssetService
            .Setup(x => x.HandleTaskWorkflowAssetMovementAsync(It.IsAny<LaberisTask>(), It.IsAny<TaskStatus>(), It.IsAny<string>()))
            .ReturnsAsync(new AssetMovementResult());

        _taskService = new TaskService(
            _mockTaskRepository.Object,
            _mockTaskEventRepository.Object,
            _mockTaskEventService.Object,
            _mockTaskStatusValidator.Object,
            _mockAssetService.Object,
            _mockWorkflowStageRepository.Object,
            _mockUserManager.Object,
            _mockProjectMembershipService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_TaskNotFound_ShouldReturnNull()
    {
        // Arrange
        const int taskId = 999;
        const string userId = "user123";

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((LaberisTask?)null);

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.Null(result);
        _mockTaskRepository.Verify(x => x.GetByIdAsync(taskId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_AlreadyInTargetStatus_ShouldReturnTaskWithoutChanges()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.CompletedAt = DateTime.UtcNow; // Already completed

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Status);
        
        // Should not call validation or event logging
        _mockTaskStatusValidator.Verify(x => x.ValidateStatusTransition(
            It.IsAny<LaberisTask>(), It.IsAny<TaskStatus>(), It.IsAny<TaskStatus>(), It.IsAny<string>()), Times.Never);
        _mockTaskEventService.Verify(x => x.LogStatusChangeEventAsync(
            It.IsAny<int>(), It.IsAny<TaskStatus>(), It.IsAny<TaskStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        const string errorMessage = "Cannot suspend a completed task";

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, It.IsAny<TaskStatus>(), TaskStatus.SUSPENDED, userId))
            .Returns((false, errorMessage));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.SUSPENDED, userId));

        Assert.Equal(errorMessage, exception.Message);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ValidSuspension_ShouldUpdateTaskAndLogEvent()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.AssignedToUserId = userId; // Make it IN_PROGRESS

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.SUSPENDED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.SUSPENDED, result.Status);
        Assert.NotNull(existingTask.SuspendedAt);
        Assert.Null(existingTask.CompletedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.ArchivedAt);
        Assert.Null(existingTask.LastWorkedOnByUserId);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ValidCompletion_ShouldUpdateTaskAndLogEvent()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.AssignedToUserId = userId; // Make it IN_PROGRESS

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act - disable asset movement to test pure status change
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId, moveAsset: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Status);
        Assert.NotNull(existingTask.CompletedAt);
        Assert.Null(existingTask.SuspendedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.ArchivedAt);
        Assert.Equal(userId, existingTask.LastWorkedOnByUserId);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ValidUnsuspension_ShouldClearSuspendedAt()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.SuspendedAt = DateTime.UtcNow.AddHours(-1); // Previously suspended
        existingTask.AssignedToUserId = userId;

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.IN_PROGRESS, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.IN_PROGRESS, result.Status);
        Assert.Null(existingTask.SuspendedAt); // Should be cleared
        Assert.Null(existingTask.CompletedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.ArchivedAt);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ArchiveCompleted_ShouldSetBothTimestamps()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "manager123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.COMPLETION);
        existingTask.CompletedAt = DateTime.UtcNow.AddHours(-1); // Previously completed

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.ARCHIVED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.ARCHIVED, result.Status);
        Assert.NotNull(existingTask.ArchivedAt);
        Assert.NotNull(existingTask.CompletedAt); // Should remain set
        Assert.Null(existingTask.SuspendedAt);
        Assert.Null(existingTask.DeferredAt);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_WithAssetMovementDisabled_ShouldNotMoveAsset()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.AssignedToUserId = userId;

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act - disable asset movement
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId, moveAsset: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Status);
        
        // Task should not be archived when asset movement is disabled
        Assert.Null(existingTask.ArchivedAt);
        Assert.NotNull(existingTask.CompletedAt);

        // Should not call methods related to workflow stage navigation
        _mockTaskRepository.Verify(x => x.GetNextWorkflowStageAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_MutualExclusion_ShouldClearConflictingTimestamps()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        
        // Set multiple conflicting timestamps
        existingTask.SuspendedAt = DateTime.UtcNow.AddHours(-2);
        existingTask.DeferredAt = DateTime.UtcNow.AddHours(-1);
        existingTask.CompletedAt = DateTime.UtcNow.AddMinutes(-30);
        existingTask.ArchivedAt = DateTime.UtcNow.AddMinutes(-10);

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, It.IsAny<TaskStatus>(), TaskStatus.SUSPENDED, userId))
            .Returns((true, string.Empty));

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.LogStatusChangeEventAsync(It.IsAny<int>(), It.IsAny<TaskStatus>(), TaskStatus.SUSPENDED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.SUSPENDED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.SUSPENDED, result.Status);
        
        // Only SuspendedAt should be set, all others cleared
        Assert.NotNull(existingTask.SuspendedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.CompletedAt);
        Assert.Null(existingTask.ArchivedAt);
    }

    private static LaberisTask CreateTestTask(int taskId, WorkflowStageType stageType)
    {
        return new LaberisTask
        {
            TaskId = taskId,
            ProjectId = 1,
            WorkflowId = 1,
            AssetId = 1,
            Priority = 1,
            CurrentWorkflowStageId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            CurrentWorkflowStage = new WorkflowStage
            {
                WorkflowStageId = 1,
                Name = "Test Stage",
                StageType = stageType,
                WorkflowId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
    }
}