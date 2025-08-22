using Microsoft.Extensions.Logging;
using Moq;
using server.Core.Alerts.Interfaces;
using server.Core.Alerts.Models;
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
/// Unit tests for the task veto pipeline.
/// Tests the backward flow: IN_PROGRESS → VETOED → Asset Transfer Back → Annotation Task Update (CHANGES_REQUIRED)
/// </summary>
public class TaskVetoPipelineTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IAssetRepository> _mockAssetRepository;
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<IWorkflowStageResolver> _mockStageResolver;
    private readonly Mock<IManagementAlertService> _mockAlertService;
    private readonly Mock<ILogger<ITaskVetoPipeline>> _mockLogger;

    public TaskVetoPipelineTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockAssetRepository = new Mock<IAssetRepository>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockStageResolver = new Mock<IWorkflowStageResolver>();
        _mockAlertService = new Mock<IManagementAlertService>();
        _mockLogger = new Mock<ILogger<ITaskVetoPipeline>>();
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidREVISIONTask_ShouldVetoSuccessfully()
    {
        // Arrange
        var taskId = 1;
        var userId = "REVISIONer123";
        var reason = "Poor annotation quality";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, userId);
        var asset = CreateTestAsset(1, 2); // Asset in REVISION data source
        var REVISIONStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var annotationTask = CreateTestTask(2, TaskStatus.COMPLETED, WorkflowStageType.ANNOTATION);

        SetupMockRepositories(REVISIONTask, asset, REVISIONStage);
        _mockStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(1, default))
            .ReturnsAsync(annotationStage);
        
        // Setup annotation task lookup
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
            .ReturnsAsync(annotationTask);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId, reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Equal(TaskStatus.VETOED, result.UpdatedTask.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidCompletionTask_ShouldVetoSuccessfully()
    {
        // Arrange
        var taskId = 1;
        var userId = "manager123";
        var reason = "Does not meet quality standards";
        var completionTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.COMPLETION, userId);
        var asset = CreateTestAsset(1, 3); // Asset in completion data source
        var completionStage = CreateTestWorkflowStage(3, WorkflowStageType.COMPLETION, 3);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var annotationTask = CreateTestTask(2, TaskStatus.COMPLETED, WorkflowStageType.ANNOTATION);

        SetupMockRepositories(completionTask, asset, completionStage);
        _mockStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(1, default))
            .ReturnsAsync(annotationStage);
    
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
            .ReturnsAsync(annotationTask);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId, reason);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Equal(TaskStatus.VETOED, result.UpdatedTask.Status);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithAnnotationTask_ShouldReturnFailure()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var annotationTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.ANNOTATION, userId);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(annotationTask);
        _mockWorkflowStageRepository.Setup(x => x.GetByIdAsync(annotationTask.WorkflowStageId))
            .ReturnsAsync(annotationStage);

        var pipeline = CreatePipeline();

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Annotation tasks cannot be vetoed", result.ErrorMessage);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithMissingAnnotationTask_ShouldCreateNewAnnotationTask()
    {
        // Arrange - Scenario: Asset imported directly to review stage, no annotation task exists
        var taskId = 1;
        var userId = "REVISIONer123";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, userId);
        var asset = CreateTestAsset(1, 2);
        var REVISIONStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);

        SetupMockRepositories(REVISIONTask, asset, REVISIONStage);
        _mockStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(1, default))
            .ReturnsAsync(annotationStage);
        
        // No annotation task found - should create new one for imported assets
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
            .ReturnsAsync((LaberisTask?)null);

        var pipeline = CreatePipeline(); // Use normal pipeline that succeeds

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Equal(TaskStatus.VETOED, result.UpdatedTask.Status);
        
        // Should NOT create a management alert (this is expected behavior for imported assets)
        _mockAlertService.Verify(x => x.CreateAlertAsync(
            It.IsAny<AlertType>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithInvalidAnnotationTaskStatus_ShouldCreateManagementAlert()
    {
        // Arrange - Scenario: Annotation task exists but has wrong status (data integrity violation)
        var taskId = 1;
        var userId = "REVISIONer123";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, userId);
        var asset = CreateTestAsset(1, 2);
        var REVISIONStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var annotationTask = CreateTestTask(2, TaskStatus.IN_PROGRESS, WorkflowStageType.ANNOTATION); // Wrong status - should be COMPLETED or VETOED

        SetupMockRepositories(REVISIONTask, asset, REVISIONStage);
        _mockStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(1, default))
            .ReturnsAsync(annotationStage);
        
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
            .ReturnsAsync(annotationTask);

        var pipeline = CreatePipelineWithFailingTaskManagementAndAlert("Invalid status: Annotation task must be COMPLETED or VETOED, but found IN_PROGRESS", taskId, asset.AssetId, userId);

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid status", result.ErrorMessage);
        
        // This IS a real data integrity violation - annotation task exists but has wrong status
        _mockAlertService.Verify(x => x.CreateAlertAsync(
            AlertType.DATA_INTEGRITY_VIOLATION,
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_WithValidAnnotationTaskStatus_ShouldSucceed()
    {
        // Arrange - Scenario: Annotation task exists with correct status (COMPLETED)
        var taskId = 1;
        var userId = "REVISIONer123";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, userId);
        var asset = CreateTestAsset(1, 2);
        var REVISIONStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var annotationStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var annotationTask = CreateTestTask(2, TaskStatus.COMPLETED, WorkflowStageType.ANNOTATION); // Correct status

        SetupMockRepositories(REVISIONTask, asset, REVISIONStage);
        _mockStageResolver.Setup(x => x.GetFirstAnnotationStageAsync(1, default))
            .ReturnsAsync(annotationStage);
        
        _mockTaskRepository.Setup(x => x.FindByAssetAndStageAsync(asset.AssetId, annotationStage.WorkflowStageId))
            .ReturnsAsync(annotationTask);

        var pipeline = CreatePipeline(); // Normal successful pipeline

        // Act
        var result = await pipeline.ExecuteAsync(taskId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UpdatedTask);
        Assert.Equal(TaskStatus.VETOED, result.UpdatedTask.Status);
        
        // Should NOT create a management alert (this is normal operation)
        _mockAlertService.Verify(x => x.CreateAlertAsync(
            It.IsAny<AlertType>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanExecuteAsync_WithREVISIONerPermissions_ShouldReturnTrue()
    {
        // Arrange
        var taskId = 1;
        var userId = "REVISIONer123";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, userId);

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(REVISIONTask);

        var pipeline = CreatePipeline();

        // Act
        var canExecute = await pipeline.CanExecuteAsync(taskId, userId);

        // Assert
        Assert.True(canExecute);
    }

    [Fact]
    public async System.Threading.Tasks.Task CanExecuteAsync_WithWrongUserAssignment_ShouldReturnFalse()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var REVISIONTask = CreateTestTask(taskId, TaskStatus.IN_PROGRESS, WorkflowStageType.REVISION, "otheruser456");

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(REVISIONTask);

        var pipeline = CreatePipeline();

        // Act
        var canExecute = await pipeline.CanExecuteAsync(taskId, userId);

        // Assert
        Assert.False(canExecute);
    }

    #region Helper Methods

    private LaberisTask CreateTestTask(int taskId, TaskStatus status, WorkflowStageType stageType, string? assignedUserId = "user123")
    {
        return new LaberisTask
        {
            TaskId = taskId,
            AssetId = 1,
            WorkflowStageId = GetStageIdForType(stageType),
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

    private int GetStageIdForType(WorkflowStageType type)
    {
        return type switch
        {
            WorkflowStageType.ANNOTATION => 1,
            WorkflowStageType.REVISION => 2,
            WorkflowStageType.COMPLETION => 3,
            _ => 1
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

    private ITaskVetoPipeline CreatePipelineWithFailingTaskManagementAndAlert(string errorMessage, int taskId, int assetId, string userId)
    {
        // Create pipeline step mocks
        var mockStatusUpdateStep = new Mock<ITaskStatusUpdateStep>();
        var mockAssetTransferStep = new Mock<IAssetTransferStep>();
        var mockTaskManagementStep = new Mock<ITaskManagementStep>();

        // Set up default successful behaviors for first two steps
        mockStatusUpdateStep.Setup(x => x.UpdateStatusAsync(It.IsAny<PipelineContext>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, TaskStatus status, CancellationToken ct) => {
                ctx.Task.Status = status;
                return ctx;
            });
        
        mockAssetTransferStep.Setup(x => x.TransferAssetToAnnotationAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);
        
        // Task management step fails with data integrity error AND creates alert
        mockTaskManagementStep.Setup(x => x.UpdateAnnotationTaskForChangesAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .Callback(() => {
                // Simulate the step creating an alert before throwing
                _mockAlertService.Object.CreateAlertAsync(
                    AlertType.DATA_INTEGRITY_VIOLATION,
                    taskId,
                    assetId,
                    userId,
                    "Invalid annotation task status",
                    errorMessage,
                    null,
                    default);
            })
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Set up rollback methods
        mockStatusUpdateStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockAssetTransferStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockTaskManagementStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockStatusUpdateStep.Setup(x => x.StepName).Returns("TaskStatusUpdateStep");
        mockAssetTransferStep.Setup(x => x.StepName).Returns("AssetTransferStep");
        mockTaskManagementStep.Setup(x => x.StepName).Returns("TaskManagementStep");

        return new TaskVetoPipeline(
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

    private ITaskVetoPipeline CreatePipelineWithFailingTaskManagement(string errorMessage)
    {
        // Create pipeline step mocks
        var mockStatusUpdateStep = new Mock<ITaskStatusUpdateStep>();
        var mockAssetTransferStep = new Mock<IAssetTransferStep>();
        var mockTaskManagementStep = new Mock<ITaskManagementStep>();

        // Set up default successful behaviors for first two steps
        mockStatusUpdateStep.Setup(x => x.UpdateStatusAsync(It.IsAny<PipelineContext>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, TaskStatus status, CancellationToken ct) => {
                ctx.Task.Status = status;
                return ctx;
            });
        
        mockAssetTransferStep.Setup(x => x.TransferAssetToAnnotationAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);
        
        // Task management step fails with data integrity error
        mockTaskManagementStep.Setup(x => x.UpdateAnnotationTaskForChangesAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(errorMessage));

        // Set up rollback methods
        mockStatusUpdateStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockAssetTransferStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockTaskManagementStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockStatusUpdateStep.Setup(x => x.StepName).Returns("TaskStatusUpdateStep");
        mockAssetTransferStep.Setup(x => x.StepName).Returns("AssetTransferStep");
        mockTaskManagementStep.Setup(x => x.StepName).Returns("TaskManagementStep");

        return new TaskVetoPipeline(
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

    private ITaskVetoPipeline CreatePipeline()
    {
        // Create pipeline step mocks
        var mockStatusUpdateStep = new Mock<ITaskStatusUpdateStep>();
        var mockAssetTransferStep = new Mock<IAssetTransferStep>();
        var mockTaskManagementStep = new Mock<ITaskManagementStep>();

        // Set up default successful behaviors for pipeline steps
        mockStatusUpdateStep.Setup(x => x.UpdateStatusAsync(It.IsAny<PipelineContext>(), It.IsAny<TaskStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, TaskStatus status, CancellationToken ct) => {
                // Update the task status in the context to reflect the change
                ctx.Task.Status = status;
                return ctx;
            });
        
        mockAssetTransferStep.Setup(x => x.TransferAssetToAnnotationAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);
        
        mockTaskManagementStep.Setup(x => x.UpdateAnnotationTaskForChangesAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PipelineContext ctx, CancellationToken ct) => ctx);

        // Set up rollback methods
        mockStatusUpdateStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockAssetTransferStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockTaskManagementStep.Setup(x => x.RollbackAsync(It.IsAny<PipelineContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockStatusUpdateStep.Setup(x => x.StepName).Returns("TaskStatusUpdateStep");
        mockAssetTransferStep.Setup(x => x.StepName).Returns("AssetTransferStep");
        mockTaskManagementStep.Setup(x => x.StepName).Returns("TaskManagementStep");

        return new TaskVetoPipeline(
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