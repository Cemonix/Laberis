using Microsoft.Extensions.Logging;
using Moq;
using server.Core.Alerts.Interfaces;
using server.Core.Workflow.Interfaces;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Core.Workflow;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using Xunit;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Tests.Core.Workflow;

/// <summary>
/// Unit tests for the task completion pipeline.
/// Tests the forward flow: IN_PROGRESS → COMPLETED → Asset Transfer → Next Stage Task Creation
/// </summary>
public class TaskCompletionPipelineTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IAssetRepository> _mockAssetRepository;
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<IWorkflowStageResolver> _mockStageResolver;
    private readonly Mock<IManagementAlertService> _mockAlertService;
    private readonly Mock<ILogger<ITaskCompletionPipeline>> _mockLogger;

    public TaskCompletionPipelineTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockAssetRepository = new Mock<IAssetRepository>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockStageResolver = new Mock<IWorkflowStageResolver>();
        _mockAlertService = new Mock<IManagementAlertService>();
        _mockLogger = new Mock<ILogger<ITaskCompletionPipeline>>();
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidTask_ShouldCompleteSuccessfully()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = CreateTestTask(taskId, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1); // Asset in annotation data source
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var nextStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);

        // Setup repositories but override task repository to return completed task on later calls
        _mockAssetRepository.Setup(x => x.GetByIdAsync(asset.AssetId))
            .ReturnsAsync(asset);
        _mockWorkflowStageRepository.Setup(x => x.GetByIdAsync(currentStage.WorkflowStageId))
            .ReturnsAsync(currentStage);
        
        // Setup task repository to return original task first, then completed task
        var callCount = 0;
        var completedTask = CreateTestTask(taskId, TaskStatus.COMPLETED, userId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
                          .ReturnsAsync(() => {
                              callCount++;
                              return callCount == 1 ? task : completedTask;
                          });
                          
        _mockStageResolver.Setup(x => x.GetNextStageAsync(currentStage.WorkflowStageId, default))
                         .ReturnsAsync(nextStage);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Equal(TaskStatus.COMPLETED, result.UpdatedTask.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithNonExistentTask_ShouldReturnFailure()
    {
        // Arrange
        var taskId = 999;
        var userId = "user123";

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync((LaberisTask?)null);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Task not found", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithInvalidTaskStatus_ShouldReturnFailure()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = CreateTestTask(taskId, TaskStatus.COMPLETED); // Already completed

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("cannot be completed", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithAssetTransferFailure_ShouldTriggerRollback()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = CreateTestTask(taskId, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var nextStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);

        SetupMockRepositories(task, asset, currentStage);
        _mockStageResolver.Setup(x => x.GetNextStageAsync(currentStage.WorkflowStageId, default))
            .ReturnsAsync(nextStage);

        // Create a pipeline with failing asset transfer step
        var pipeline = CreateFailingAssetTransferPipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Asset transfer failed", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithFinalStage_ShouldNotCreateNextTask()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var finalStage = CreateTestWorkflowStage(3, WorkflowStageType.COMPLETION, 3);
        var task = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, finalStage.WorkflowStageId, userId);
        var asset = CreateTestAsset(1, 3); // Asset in completion data source

        // Setup repositories but override task repository for completion tracking
        _mockAssetRepository.Setup(x => x.GetByIdAsync(asset.AssetId))
            .ReturnsAsync(asset);
        _mockWorkflowStageRepository.Setup(x => x.GetByIdAsync(finalStage.WorkflowStageId))
            .ReturnsAsync(finalStage);
        
        // Setup task repository to return original task first, then completed task
        var callCount = 0;
        var completedTask = CreateTestTask(taskId, TaskStatus.COMPLETED, finalStage.WorkflowStageId, userId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
                          .ReturnsAsync(() => {
                              callCount++;
                              return callCount == 1 ? task : completedTask;
                          });
                          
        _mockStageResolver.Setup(x => x.GetNextStageAsync(finalStage.WorkflowStageId, default))
            .ReturnsAsync((WorkflowStage?)null); // No next stage

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Null(result.CreatedTask); // No next task created
        Assert.Equal(TaskStatus.COMPLETED, result.UpdatedTask.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanExecuteAsync_WithValidPermissions_ShouldReturnTrue()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, userId);

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        var pipeline = CreatePipeline();

        // Act
        var canExecute = await pipeline.CanExecuteAsync(taskId, userId);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanExecuteAsync_WithUnassignedTask_ShouldReturnFalse()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, null); // Not assigned

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        var pipeline = CreatePipeline();

        // Act
        var canExecute = await pipeline.CanExecuteAsync(taskId, userId);

        // Assert
        Assert.False(canExecute);
    }

    #region Helper Methods

    private static LaberisTask CreateTestTask(int taskId, TaskStatus status, string? assignedUserId = "user123")
    {
        return CreateTestTask(taskId, status, 1, assignedUserId); // Default to stage 1
    }

    private static LaberisTask CreateTestTask(int taskId, TaskStatus status, int workflowStageId, string? assignedUserId = "user123")
    {
        return new LaberisTask
        {
            TaskId = taskId,
            AssetId = 1,
            WorkflowStageId = workflowStageId,
            ProjectId = 1,
            Status = status,
            AssignedToUserId = assignedUserId,
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static WorkflowStage CreateTestWorkflowStage(int stageId, WorkflowStageType type, int? targetDataSourceId = null)
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

    private void SetupMockRepositories(LaberisTask task, Asset asset, WorkflowStage stage)
    {
        _mockTaskRepository.Setup(x => x.GetByIdAsync(task.TaskId))
            .ReturnsAsync(task);
        
        _mockAssetRepository.Setup(x => x.GetByIdAsync(asset.AssetId))
            .ReturnsAsync(asset);
        
        _mockWorkflowStageRepository.Setup(x => x.GetByIdAsync(stage.WorkflowStageId))
            .ReturnsAsync(stage);
    }

    private ITaskCompletionPipeline CreatePipeline()
    {
        // Create mock steps
        var mockStatusUpdateStep = new Mock<ITaskStatusUpdateStep>();
        var mockAssetTransferStep = new Mock<IAssetTransferStep>();
        var mockTaskManagementStep = new Mock<ITaskManagementStep>();

        // Setup default successful behaviors
        mockStatusUpdateStep.Setup(x => x.UpdateStatusAsync(It.IsAny<PipelineContext>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((PipelineContext ctx, TaskStatus status, CancellationToken ct) => {
                               // Update the task status in the context
                               ctx.Task.Status = status;
                               return ctx;
                           });
        
        mockAssetTransferStep.Setup(x => x.TransferAssetAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);
        
        mockTaskManagementStep.Setup(x => x.CreateOrUpdateTaskForTargetStageAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);

        return new TaskCompletionPipeline(
            _mockTaskRepository.Object,
            _mockAssetRepository.Object,
            _mockWorkflowStageRepository.Object,
            _mockStageResolver.Object,
            mockStatusUpdateStep.Object,
            mockAssetTransferStep.Object,
            mockTaskManagementStep.Object,
            _mockAlertService.Object,
            _mockLogger.Object);
    }

    private ITaskCompletionPipeline CreateFailingAssetTransferPipeline()
    {
        // Create mock steps with failing asset transfer
        var mockStatusUpdateStep = new Mock<ITaskStatusUpdateStep>();
        var mockAssetTransferStep = new Mock<IAssetTransferStep>();
        var mockTaskManagementStep = new Mock<ITaskManagementStep>();

        // Status update succeeds
        mockStatusUpdateStep.Setup(x => x.UpdateStatusAsync(It.IsAny<PipelineContext>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((PipelineContext ctx, TaskStatus status, CancellationToken ct) => {
                               ctx.Task.Status = status;
                               return ctx;
                           });
        
        // Asset transfer fails
        mockAssetTransferStep.Setup(x => x.TransferAssetAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new InvalidOperationException("Asset transfer failed"));
        
        // Task management shouldn't be called due to early failure
        mockTaskManagementStep.Setup(x => x.CreateOrUpdateTaskForTargetStageAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);

        // Setup rollback behaviors
        mockStatusUpdateStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(true);

        return new TaskCompletionPipeline(
            _mockTaskRepository.Object,
            _mockAssetRepository.Object,
            _mockWorkflowStageRepository.Object,
            _mockStageResolver.Object,
            mockStatusUpdateStep.Object,
            mockAssetTransferStep.Object,
            mockTaskManagementStep.Object,
            _mockAlertService.Object,
            _mockLogger.Object);
    }

    #endregion
}