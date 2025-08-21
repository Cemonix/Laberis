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
    private readonly Mock<IAssetRepository> _mockAssetRepository;
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
        _mockAssetRepository = new Mock<IAssetRepository>();
        _mockTaskEventService = new Mock<ITaskEventService>();
        _mockTaskStatusValidator = new Mock<ITaskStatusValidator>();
        _mockAssetService = new Mock<IAssetService>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockUserManager = MockUserManager();
        _mockProjectMembershipService = new Mock<IProjectMembershipService>();
        _mockLogger = new Mock<ILogger<TaskService>>();


        _taskService = new TaskService(
            _mockTaskRepository.Object,
            _mockAssetRepository.Object,
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
        existingTask.Status = TaskStatus.COMPLETED; // Set status explicitly
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
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(
            It.IsAny<int>(), It.IsAny<TaskStatus>(), It.IsAny<TaskStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.Status = TaskStatus.COMPLETED; // Set status explicitly
        const string errorMessage = "Cannot change to SUSPENDED from COMPLETED. SUSPENDED status changes are not allowed via direct status transitions.";

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.COMPLETED, TaskStatus.SUSPENDED, userId))
            .Returns(false);

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
        existingTask.Status = TaskStatus.IN_PROGRESS; // Set status explicitly
        existingTask.AssignedToUserId = userId; // Make it IN_PROGRESS
        existingTask.LastWorkedOnByUserId = userId; // Task has been worked on

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId))
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
        Assert.Equal(userId, existingTask.LastWorkedOnByUserId); // Should preserve who last worked on it

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ValidCompletion_ShouldUpdateTaskAndLogEvent()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.Status = TaskStatus.IN_PROGRESS; // Set status explicitly
        existingTask.AssignedToUserId = userId; // Make it IN_PROGRESS
        existingTask.LastWorkedOnByUserId = userId; // Task has been worked on

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act - disable asset movement to test pure status change
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Status);
        Assert.NotNull(existingTask.CompletedAt);
        Assert.Null(existingTask.SuspendedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.ArchivedAt);
        Assert.Equal(userId, existingTask.LastWorkedOnByUserId);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ValidUnsuspension_ShouldClearSuspendedAt()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.Status = TaskStatus.SUSPENDED; // Set status explicitly
        existingTask.SuspendedAt = DateTime.UtcNow.AddHours(-1); // Previously suspended
        existingTask.AssignedToUserId = userId;

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.IN_PROGRESS, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.IN_PROGRESS, result.Status);
        Assert.NotNull(existingTask.SuspendedAt); // Timestamp is preserved
        Assert.Null(existingTask.CompletedAt);
        Assert.Null(existingTask.DeferredAt);
        Assert.Null(existingTask.ArchivedAt);

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_ArchiveCompleted_ShouldSetBothTimestamps()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "manager123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.COMPLETION);
        existingTask.Status = TaskStatus.COMPLETED; // Set status explicitly
        existingTask.CompletedAt = DateTime.UtcNow.AddHours(-1); // Previously completed

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId))
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
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.COMPLETED, TaskStatus.ARCHIVED, userId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_WithAssetMovementDisabled_ShouldNotMoveAsset()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.Status = TaskStatus.IN_PROGRESS; // Set status explicitly
        existingTask.AssignedToUserId = userId;
        existingTask.LastWorkedOnByUserId = userId; // Task has been worked on

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act - disable asset movement
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Status);
        
        // Task should not be archived when asset movement is disabled
        Assert.Null(existingTask.ArchivedAt);
        Assert.NotNull(existingTask.CompletedAt);

        // Should not call methods related to workflow stage navigation
        _mockWorkflowStageRepository.Verify(x => x.GetNextWorkflowStageAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ChangeTaskStatusAsync_SuspendedChangesRequiredToInProgress_ShouldHandleChangesRequiredCorrectly()
    {
        // Arrange - This tests the exact issue: CHANGES_REQUIRED → SUSPENDED → IN_PROGRESS (frontend call)
        const int taskId = 1;
        const string userId = "user123";
        var originalChangesRequiredTime = DateTime.UtcNow.AddDays(-1);

        var existingTask = CreateTestTask(taskId, WorkflowStageType.ANNOTATION);
        existingTask.Status = TaskStatus.SUSPENDED; // Set status explicitly
        existingTask.AssignedToUserId = userId;
        existingTask.LastWorkedOnByUserId = userId;
        existingTask.SuspendedAt = DateTime.UtcNow.AddHours(-1); // Currently suspended
        existingTask.ChangesRequiredAt = originalChangesRequiredTime; // Originally had changes required

        _mockTaskRepository
            .Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(existingTask);

        _mockTaskStatusValidator
            .Setup(x => x.ValidateStatusTransition(existingTask, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .Returns(true);

        _mockTaskRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mockTaskEventService
            .Setup(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId))
            .ReturnsAsync(new TaskEventDto { Id = 1, EventType = TaskEventType.STATUS_CHANGED });

        // Act - Frontend explicitly changes status to IN_PROGRESS when starting annotation
        var result = await _taskService.ChangeTaskStatusAsync(taskId, TaskStatus.IN_PROGRESS, userId);

        // Assert
        Assert.NotNull(result);
        // Task transitions to IN_PROGRESS as requested
        Assert.Equal(TaskStatus.IN_PROGRESS, result.Status);
        
        // Veto tracking: ChangesRequiredAt should be preserved for proper workflow tracking
        Assert.NotNull(existingTask.ChangesRequiredAt);
        Assert.Equal(originalChangesRequiredTime, existingTask.ChangesRequiredAt);
        
        Assert.NotNull(existingTask.SuspendedAt); // Timestamp is preserved
        Assert.Equal(userId, existingTask.AssignedToUserId); // Should remain assigned
        Assert.Equal(userId, existingTask.LastWorkedOnByUserId); // Should be set since it's IN_PROGRESS

        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskEventService.Verify(x => x.CreateStatusChangeEventAsync(taskId, TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, userId), Times.Once);
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
            WorkflowStageId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            WorkflowStage = new WorkflowStage
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
        var optionsAccessor = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var passwordValidators = new List<IPasswordValidator<ApplicationUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<ApplicationUser>>>();
        
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, 
            optionsAccessor.Object, 
            passwordHasher.Object, 
            userValidators, 
            passwordValidators, 
            keyNormalizer.Object, 
            errors.Object, 
            services.Object, 
            logger.Object);
    }
}