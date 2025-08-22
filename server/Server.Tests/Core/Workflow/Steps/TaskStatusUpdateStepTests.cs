using Microsoft.Extensions.Logging;
using Moq;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Core.Workflow.Steps;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using Xunit;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Tests.Core.Workflow.Steps;

/// <summary>
/// Unit tests for TaskStatusUpdateStep - the pipeline step responsible for updating task status.
/// Uses TDD approach: tests first, then implementation.
/// </summary>
public class TaskStatusUpdateStepTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ILogger<ITaskStatusUpdateStep>> _mockLogger;

    public TaskStatusUpdateStepTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockLogger = new Mock<ILogger<ITaskStatusUpdateStep>>();
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidContext_ShouldUpdateTaskStatusToCompleted()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(task, asset, stage, "user123");

        var updatedTask = CreateTestTask(1, TaskStatus.COMPLETED);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(task, TaskStatus.COMPLETED, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.UpdateStatusAsync(context, TaskStatus.COMPLETED);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.COMPLETED, result.Task.Status);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(task, TaskStatus.COMPLETED, "user123"), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidContext_ShouldUpdateTaskStatusToVetoed()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 2);
        var stage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION);
        var context = new PipelineContext(task, asset, stage, "reviewer123");

        var updatedTask = CreateTestTask(1, TaskStatus.VETOED);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(task, TaskStatus.VETOED, "reviewer123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.UpdateStatusAsync(context, TaskStatus.VETOED);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskStatus.VETOED, result.Task.Status);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(task, TaskStatus.VETOED, "reviewer123"), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithRepositoryFailure_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(task, asset, stage, "user123");

        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(task, TaskStatus.COMPLETED, "user123"))
                          .ThrowsAsync(new InvalidOperationException("Database error"));

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.UpdateStatusAsync(context, TaskStatus.COMPLETED));
        
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_UsingBaseInterfaceMethod_ShouldDelegateToUpdateStatusAsync()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(task, asset, stage, "user123");

        var updatedTask = CreateTestTask(1, TaskStatus.COMPLETED);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(task, TaskStatus.COMPLETED, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act - using the base IPipelineStep interface method
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);
        // Note: Default behavior should update to COMPLETED status for forward flow
        Assert.Equal(TaskStatus.COMPLETED, result.Task.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithValidContext_ShouldRestoreOriginalStatus()
    {
        // Arrange
        var originalTask = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(originalTask, asset, stage, "user123");

        var updatedTask = CreateTestTask(1, TaskStatus.COMPLETED);
        var rolledBackTask = CreateTestTask(1, TaskStatus.IN_PROGRESS);

        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(originalTask, TaskStatus.COMPLETED, "user123"))
                          .ReturnsAsync(updatedTask);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(updatedTask, TaskStatus.IN_PROGRESS, "user123"))
                          .ReturnsAsync(rolledBackTask);

        var step = CreateStep();

        // Act - First execute a status update to store original status
        var updatedContext = await step.UpdateStatusAsync(context, TaskStatus.COMPLETED);
        // Then rollback
        var result = await step.RollbackAsync(updatedContext);

        // Assert
        Assert.True(result);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(updatedTask, TaskStatus.IN_PROGRESS, "user123"), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithRepositoryFailure_ShouldReturnFalse()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(task, asset, stage, "user123");

        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(It.IsAny<LaberisTask>(), It.IsAny<TaskStatus>(), It.IsAny<string>()))
                          .ThrowsAsync(new Exception("Rollback failed"));

        var step = CreateStep();

        // Act
        var result = await step.RollbackAsync(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void StepName_ShouldReturnCorrectName()
    {
        // Arrange
        var step = CreateStep();

        // Act
        var stepName = step.StepName;

        // Assert
        Assert.Equal("TaskStatusUpdateStep", stepName);
    }

    [Theory]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED)]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED)]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.VETOED)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.CHANGES_REQUIRED)]
    public async System.Threading.Tasks.Task UpdateStatusAsync_WithVariousStatusTransitions_ShouldHandleCorrectly(
        TaskStatus fromStatus, TaskStatus toStatus)
    {
        // Arrange
        var task = CreateTestTask(1, fromStatus);
        var asset = CreateTestAsset(1, 1);
        var stage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION);
        var context = new PipelineContext(task, asset, stage, "user123");

        var updatedTask = CreateTestTask(1, toStatus);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(task, toStatus, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.UpdateStatusAsync(context, toStatus);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(toStatus, result.Task.Status);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(task, toStatus, "user123"), Times.Once);
    }

    #region Helper Methods

    private static LaberisTask CreateTestTask(int taskId, TaskStatus status)
    {
        return new LaberisTask
        {
            TaskId = taskId,
            AssetId = 1,
            WorkflowStageId = 1,
            ProjectId = 1,
            Status = status,
            AssignedToUserId = "user123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Asset CreateTestAsset(int assetId, int dataSourceId)
    {
        return new Asset
        {
            AssetId = assetId,
            DataSourceId = dataSourceId,
            Filename = "test.jpg",
            Status = AssetStatus.IMPORTED,
            ProjectId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static WorkflowStage CreateTestWorkflowStage(int stageId, WorkflowStageType type)
    {
        return new WorkflowStage
        {
            WorkflowStageId = stageId,
            WorkflowId = 1,
            Name = $"{type} Stage",
            StageType = type,
            TargetDataSourceId = stageId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private ITaskStatusUpdateStep CreateStep()
    {
        return new TaskStatusUpdateStep(_mockTaskRepository.Object, _mockLogger.Object);
    }

    #endregion
}