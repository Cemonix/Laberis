using Microsoft.Extensions.Logging;
using Moq;
using server.Core.Workflow.Interfaces;
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
/// Unit tests for TaskManagementStep - the pipeline step responsible for managing task creation and updates.
/// Uses TDD approach: tests first, then implementation.
/// </summary>
public class TaskManagementStepTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<IWorkflowStageResolver> _mockWorkflowStageResolver;
    private readonly Mock<ILogger<ITaskManagementStep>> _mockLogger;

    public TaskManagementStepTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockWorkflowStageResolver = new Mock<IWorkflowStageResolver>();
        _mockLogger = new Mock<ILogger<ITaskManagementStep>>();
    }

    #region CreateOrUpdateTaskForTargetStageAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithNoExistingTask_ShouldCreateNewTask()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // No existing task for the asset and target stage
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync((LaberisTask?)null);

        var newTask = CreateTestTask(2, TaskStatus.NOT_STARTED);
        newTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.AddAsync(It.IsAny<LaberisTask>()))
                          .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(1);

        var step = CreateStep();

        // Act
        var result = await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetStage, result.TargetStage);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.AddAsync(It.IsAny<LaberisTask>()), Times.Once);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(It.IsAny<LaberisTask>(), It.IsAny<TaskStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithExistingCompletedTask_ShouldUpdateToReadyForReview()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // Existing task in COMPLETED state
        var existingTask = CreateTestTask(2, TaskStatus.COMPLETED);
        existingTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync(existingTask);

        var updatedTask = CreateTestTask(2, TaskStatus.READY_FOR_REVIEW);
        updatedTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_REVIEW, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_REVIEW, "user123"), Times.Once);
        _mockTaskRepository.Verify(x => x.AddAsync(It.IsAny<LaberisTask>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithNullTargetStage_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var context = new PipelineContext(task, asset, currentStage, "user123"); // No target stage

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.CreateOrUpdateTaskForTargetStageAsync(context));
        
        Assert.Contains("Target stage is required", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithRepositoryFailure_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.CreateOrUpdateTaskForTargetStageAsync(context));
        
        Assert.Equal("Database connection failed", exception.Message);
    }

    #endregion

    #region UpdateAnnotationTaskForChangesAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task UpdateAnnotationTaskForChangesAsync_WithExistingAnnotationTask_ShouldUpdateToChangesRequired()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.VETOED);
        var asset = CreateTestAsset(1, 2); // Asset currently in review stage
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "reviewer123");

        // Mock finding the first annotation stage
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        _mockWorkflowStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(currentStage.WorkflowId, default))
                                   .ReturnsAsync(annotationStage);

        // Mock finding the annotation task
        var annotationTask = CreateTestTask(3, TaskStatus.COMPLETED);
        annotationTask.WorkflowStageId = annotationStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
                          .ReturnsAsync(annotationTask);

        // Mock updating the task status
        var updatedTask = CreateTestTask(3, TaskStatus.CHANGES_REQUIRED);
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(annotationTask, TaskStatus.CHANGES_REQUIRED, "reviewer123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.UpdateAnnotationTaskForChangesAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockWorkflowStageResolver.Verify(x => x.GetFirstAnnotationStageAsync(currentStage.WorkflowId, default), Times.Once);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(annotationTask, TaskStatus.CHANGES_REQUIRED, "reviewer123"), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAnnotationTaskForChangesAsync_WithNoInitialStage_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.VETOED);
        var asset = CreateTestAsset(1, 2);
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "reviewer123");

        _mockWorkflowStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(currentStage.WorkflowId, default))
                                   .ReturnsAsync((WorkflowStage?)null);

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.UpdateAnnotationTaskForChangesAsync(context));
        
        Assert.Contains("First annotation stage not found", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAnnotationTaskForChangesAsync_WithNoAnnotationTask_ShouldCreateNewTask()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.VETOED);
        var asset = CreateTestAsset(1, 2);
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "reviewer123");

        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        _mockWorkflowStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(currentStage.WorkflowId, default))
                                   .ReturnsAsync(annotationStage);

        // No annotation task exists (imported asset scenario)
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
                          .ReturnsAsync((LaberisTask?)null);

        // Mock task creation
        _mockTaskRepository.Setup(x => x.AddAsync(It.IsAny<LaberisTask>()))
                          .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(1);

        var step = CreateStep();

        // Act
        var result = await step.UpdateAnnotationTaskForChangesAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.AddAsync(It.Is<LaberisTask>(t => 
            t.AssetId == asset.AssetId && 
            t.WorkflowStageId == annotationStage.WorkflowStageId &&
            t.Status == TaskStatus.CHANGES_REQUIRED
        )), Times.Once);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithAnnotationStage_ShouldUpdateToReadyForAnnotation()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var targetStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // Existing task in VETOED state
        var existingTask = CreateTestTask(2, TaskStatus.VETOED);
        existingTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync(existingTask);

        var updatedTask = CreateTestTask(2, TaskStatus.READY_FOR_ANNOTATION);
        updatedTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_ANNOTATION, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_ANNOTATION, "user123"), Times.Once);
        _mockTaskRepository.Verify(x => x.AddAsync(It.IsAny<LaberisTask>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateOrUpdateTaskForTargetStageAsync_WithCompletionStage_ShouldUpdateToReadyForCompletion()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var targetStage = CreateTestWorkflowStage(3, WorkflowStageType.COMPLETION, 3);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // Existing task in COMPLETED state
        var existingTask = CreateTestTask(3, TaskStatus.COMPLETED);
        existingTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync(existingTask);

        var updatedTask = CreateTestTask(3, TaskStatus.READY_FOR_COMPLETION);
        updatedTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_COMPLETION, "user123"))
                          .ReturnsAsync(updatedTask);

        var step = CreateStep();

        // Act
        var result = await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId), Times.Once);
        _mockTaskRepository.Verify(x => x.UpdateTaskStatusAsync(existingTask, TaskStatus.READY_FOR_COMPLETION, "user123"), Times.Once);
        _mockTaskRepository.Verify(x => x.AddAsync(It.IsAny<LaberisTask>()), Times.Never);
    }

    #endregion

    #region ValidateDataIntegrityAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task ValidateDataIntegrityAsync_WithValidState_ShouldReturnTrue()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        var targetTask = CreateTestTask(2, TaskStatus.NOT_STARTED);
        targetTask.WorkflowStageId = targetStage.WorkflowStageId;

        // Mock getting all tasks for the asset
        var allTasks = new List<LaberisTask> { task, targetTask };
        _mockTaskRepository.Setup(x => x.GetTasksByAssetIdAsync(asset.AssetId))
                          .ReturnsAsync(allTasks);

        var step = CreateStep();

        // Act
        var result = await step.ValidateDataIntegrityAsync(context, targetTask);

        // Assert
        Assert.True(result);
        _mockTaskRepository.Verify(x => x.GetTasksByAssetIdAsync(asset.AssetId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ValidateDataIntegrityAsync_WithConflictingTasks_ShouldReturnFalse()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        var targetTask = CreateTestTask(2, TaskStatus.IN_PROGRESS);
        targetTask.WorkflowStageId = targetStage.WorkflowStageId;

        // Two IN_PROGRESS tasks should be a conflict
        var allTasks = new List<LaberisTask> { task, targetTask };
        _mockTaskRepository.Setup(x => x.GetTasksByAssetIdAsync(asset.AssetId))
                          .ReturnsAsync(allTasks);

        var step = CreateStep();

        // Act
        var result = await step.ValidateDataIntegrityAsync(context, targetTask);

        // Assert
        Assert.False(result);
        _mockTaskRepository.Verify(x => x.GetTasksByAssetIdAsync(asset.AssetId), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ValidateDataIntegrityAsync_WithNullTargetTask_ShouldReturnTrue()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var context = new PipelineContext(task, asset, currentStage, "user123");

        var allTasks = new List<LaberisTask> { task };
        _mockTaskRepository.Setup(x => x.GetTasksByAssetIdAsync(asset.AssetId))
                          .ReturnsAsync(allTasks);

        var step = CreateStep();

        // Act
        var result = await step.ValidateDataIntegrityAsync(context, null);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldDelegateToCreateOrUpdateTaskForTargetStageAsync()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync((LaberisTask?)null);

        _mockTaskRepository.Setup(x => x.AddAsync(It.IsAny<LaberisTask>()))
                          .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(1);

        var step = CreateStep();

        // Act - using the base IPipelineStep interface method
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockTaskRepository.Verify(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId), Times.Once);
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithCreatedTask_ShouldDeleteTask()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // First, simulate the creation of a task (which would set _createdTaskId)
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync((LaberisTask?)null); // No existing task
        
        _mockTaskRepository.Setup(x => x.AddAsync(It.IsAny<LaberisTask>()))
                          .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(1);

        var step = CreateStep();

        // First execute the step to create a task
        await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Now mock the task retrieval for rollback
        var createdTask = CreateTestTask(2, TaskStatus.NOT_STARTED);
        createdTask.WorkflowStageId = targetStage.WorkflowStageId;
        _mockTaskRepository.Setup(x => x.GetByIdAsync(It.IsAny<object>()))
                          .ReturnsAsync(createdTask);

        // Act - perform rollback
        var result = await step.RollbackAsync(context);

        // Assert
        Assert.True(result);
        _mockTaskRepository.Verify(x => x.GetByIdAsync(It.IsAny<object>()), Times.Once);
        _mockTaskRepository.Verify(x => x.Remove(createdTask), Times.Once);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithDeletionFailure_ShouldReturnFalse()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.COMPLETED);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        // First, simulate the creation of a task (which would set _createdTaskId)
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, targetStage.WorkflowStageId))
                          .ReturnsAsync((LaberisTask?)null); // No existing task
        
        _mockTaskRepository.Setup(x => x.AddAsync(It.IsAny<LaberisTask>()))
                          .Returns(System.Threading.Tasks.Task.CompletedTask);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(1);

        var step = CreateStep();

        // First execute the step to create a task
        await step.CreateOrUpdateTaskForTargetStageAsync(context);

        // Now mock the task retrieval for rollback - but make the save fail
        var createdTask = CreateTestTask(2, TaskStatus.NOT_STARTED);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(It.IsAny<object>()))
                          .ReturnsAsync(createdTask);

        // Setup SaveChangesAsync to return 0 (indicating failure) for the rollback operation
        _mockTaskRepository.Setup(x => x.SaveChangesAsync())
                          .ReturnsAsync(0);

        // Act
        var result = await step.RollbackAsync(context);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region StepName Test

    [Fact]
    public void StepName_ShouldReturnCorrectName()
    {
        // Arrange
        var step = CreateStep();

        // Act
        var stepName = step.StepName;

        // Assert
        Assert.Equal("TaskManagementStep", stepName);
    }

    #endregion

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

    private static WorkflowStage CreateTestWorkflowStage(int stageId, WorkflowStageType type, int? targetDataSourceId)
    {
        return new WorkflowStage
        {
            WorkflowStageId = stageId,
            WorkflowId = 1,
            Name = $"{type} Stage",
            StageType = type,
            TargetDataSourceId = targetDataSourceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private ITaskManagementStep CreateStep()
    {
        return new TaskManagementStep(_mockTaskRepository.Object, _mockWorkflowStageRepository.Object, _mockWorkflowStageResolver.Object, _mockLogger.Object);
    }

    #endregion
}