using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Services;
using Xunit;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace Server.Tests.Services;

public class TaskStatusValidatorTests
{
    private readonly Mock<ILogger<TaskStatusValidator>> _mockLogger;
    private readonly TaskStatusValidator _validator;

    public TaskStatusValidatorTests()
    {
        _mockLogger = new Mock<ILogger<TaskStatusValidator>>();
        _validator = new TaskStatusValidator(_mockLogger.Object);
    }

    [Theory]
    [InlineData(TaskStatus.READY_FOR_ANNOTATION, TaskStatus.IN_PROGRESS, true)]
    [InlineData(TaskStatus.READY_FOR_REVIEW, TaskStatus.IN_PROGRESS, true)]
    [InlineData(TaskStatus.READY_FOR_COMPLETION, TaskStatus.IN_PROGRESS, true)]
    [InlineData(TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, true)]
    [InlineData(TaskStatus.NOT_STARTED, TaskStatus.IN_PROGRESS, true)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.IN_PROGRESS, false)]
    [InlineData(TaskStatus.ARCHIVED, TaskStatus.IN_PROGRESS, false)]
    public void ValidateStatusTransition_ToInProgress_ShouldValidateCorrectly(
        TaskStatus currentStatus, TaskStatus targetStatus, bool expectedValid)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, currentStatus, targetStatus, "user123");

        // Assert
        Assert.Equal(expectedValid, IsValid);
    }

    [Theory]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, true)]
    [InlineData(TaskStatus.READY_FOR_ANNOTATION, TaskStatus.COMPLETED, false)]
    [InlineData(TaskStatus.SUSPENDED, TaskStatus.COMPLETED, false)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.COMPLETED, false)]
    public void ValidateStatusTransition_ToCompleted_ShouldValidateCorrectly(
        TaskStatus currentStatus, TaskStatus targetStatus, bool expectedValid)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, currentStatus, targetStatus, "user123");

        // Assert
        Assert.Equal(expectedValid, IsValid);
    }

    [Theory]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, true)]
    [InlineData(TaskStatus.READY_FOR_ANNOTATION, TaskStatus.SUSPENDED, true)]
    [InlineData(TaskStatus.READY_FOR_REVIEW, TaskStatus.SUSPENDED, true)]
    [InlineData(TaskStatus.READY_FOR_COMPLETION, TaskStatus.SUSPENDED, true)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.SUSPENDED, false)]
    [InlineData(TaskStatus.ARCHIVED, TaskStatus.SUSPENDED, false)]
    public void ValidateStatusTransition_ToSuspended_ShouldValidateCorrectly(
        TaskStatus currentStatus, TaskStatus targetStatus, bool expectedValid)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, currentStatus, targetStatus, "user123");

        // Assert
        Assert.Equal(expectedValid, IsValid);
    }

    [Theory]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.DEFERRED, true)]
    [InlineData(TaskStatus.READY_FOR_ANNOTATION, TaskStatus.DEFERRED, true)]
    [InlineData(TaskStatus.READY_FOR_REVIEW, TaskStatus.DEFERRED, true)]
    [InlineData(TaskStatus.READY_FOR_COMPLETION, TaskStatus.DEFERRED, true)]
    [InlineData(TaskStatus.NOT_STARTED, TaskStatus.DEFERRED, true)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.DEFERRED, false)]
    [InlineData(TaskStatus.ARCHIVED, TaskStatus.DEFERRED, false)]
    [InlineData(TaskStatus.SUSPENDED, TaskStatus.DEFERRED, false)]
    public void ValidateStatusTransition_ToDeferred_ShouldValidateCorrectly(
        TaskStatus currentStatus, TaskStatus targetStatus, bool expectedValid)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, currentStatus, targetStatus, "user123");

        // Assert
        Assert.Equal(expectedValid, IsValid);
    }

    [Theory]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.ARCHIVED, true)]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.ARCHIVED, false)]
    [InlineData(TaskStatus.SUSPENDED, TaskStatus.ARCHIVED, false)]
    public void ValidateStatusTransition_ToArchived_ShouldValidateCorrectly(
        TaskStatus currentStatus, TaskStatus targetStatus, bool expectedValid)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.COMPLETION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, currentStatus, targetStatus, "user123");

        // Assert
        Assert.Equal(expectedValid, IsValid);
    }

    [Fact]
    public void ValidateStatusTransition_CompletedToReadyForAnnotation_InCompletionStage_ShouldBeInvalid()
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.COMPLETION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, TaskStatus.COMPLETED, TaskStatus.READY_FOR_ANNOTATION, "manager123");

        // Assert - This should now be invalid, use return-for-rework instead
        Assert.False(IsValid);
        // Note: Detailed error messages not available with bool return type
    }

    [Fact]
    public void ValidateStatusTransition_CompletedToReadyForAnnotation_InAnnotationStage_ShouldBeInvalid()
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, TaskStatus.COMPLETED, TaskStatus.READY_FOR_ANNOTATION, "manager123");

        // Assert
        Assert.False(IsValid);
        // Note: Detailed error messages not available with bool return type
    }

    [Theory]
    [InlineData(TaskStatus.READY_FOR_REVIEW)]
    [InlineData(TaskStatus.READY_FOR_COMPLETION)]
    [InlineData(TaskStatus.NOT_STARTED)]
    public void ValidateStatusTransition_ToAutomaticStatuses_ShouldBeInvalid(TaskStatus targetStatus)
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, TaskStatus.IN_PROGRESS, targetStatus, "user123");

        // Assert
        Assert.False(IsValid);
        // Note: Detailed error messages not available with bool return type
    }

    [Fact]
    public void ValidateStatusTransition_UnknownTargetStatus_ShouldReturnError()
    {
        // Arrange
        var task = CreateTestTask(WorkflowStageType.ANNOTATION);
        var unknownStatus = (TaskStatus)999;

        // Act
        var IsValid = _validator.ValidateStatusTransition(task, TaskStatus.IN_PROGRESS, unknownStatus, "user123");

        // Assert
        Assert.False(IsValid);
        // Note: Detailed error messages not available with bool return type
    }

    private static LaberisTask CreateTestTask(WorkflowStageType stageType)
    {
        return new LaberisTask
        {
            TaskId = 1,
            ProjectId = 1,
            WorkflowId = 1,
            AssetId = 1,
            Priority = 1,
            CurrentWorkflowStageId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CurrentWorkflowStage = new WorkflowStage
            {
                WorkflowStageId = 1,
                Name = "Test Stage",
                StageType = stageType,
                WorkflowId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }
}