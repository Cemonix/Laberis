using Microsoft.Extensions.Logging;
using Moq;
using server.Core.Workflow.Interfaces.Steps;
using server.Core.Workflow.Models;
using server.Core.Workflow.Steps;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.DataSource;
using server.Models.Internal;
using server.Services.Interfaces;
using Xunit;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Tests.Core.Workflow.Steps;

/// <summary>
/// Unit tests for AssetTransferStep - the pipeline step responsible for transferring assets between data sources.
/// Uses TDD approach: tests first, then implementation.
/// </summary>
public class AssetTransferStepTests
{
    private readonly Mock<IAssetService> _mockAssetService;
    private readonly Mock<IDataSourceService> _mockDataSourceService;
    private readonly Mock<ILogger<IAssetTransferStep>> _mockLogger;

    public AssetTransferStepTests()
    {
        _mockAssetService = new Mock<IAssetService>();
        _mockDataSourceService = new Mock<IDataSourceService>();
        _mockLogger = new Mock<ILogger<IAssetTransferStep>>();
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetAsync_WithValidContext_ShouldTransferToTargetDataSource()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1); // Asset in annotation data source
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value))
                        .ReturnsAsync(true);

        var step = CreateStep();

        // Act
        var result = await step.TransferAssetAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetStage, result.TargetStage);
        _mockAssetService.Verify(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetAsync_WithTransferFailure_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value))
                        .ReturnsAsync(false);

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.TransferAssetAsync(context));
        
        Assert.Contains("Failed to transfer asset", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetAsync_WithNullTargetStage_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var context = new PipelineContext(task, asset, currentStage, "user123"); // No target stage

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.TransferAssetAsync(context));
        
        Assert.Contains("Target stage is required", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetAsync_WithNullTargetDataSourceId_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.COMPLETION, null); // No target data source
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.TransferAssetAsync(context));
        
        Assert.Contains("Target data source is required", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetToAnnotationAsync_WithValidContext_ShouldTransferToFirstAnnotationStage()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.VETOED);
        var asset = CreateTestAsset(1, 2); // Asset currently in review data source
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "reviewer123");

        // Mock data source service to return annotation data source
        var mockDataSource = new DataSourceDto { Id = 1, Name = "Test Annotation Source", IsDefault = true, ProjectId = asset.ProjectId, SourceType = DataSourceType.MINIO_BUCKET, Status = DataSourceStatus.ACTIVE, CreatedAt = DateTime.UtcNow, AssetCount = 10 };
        var mockWorkflowDataSources = new WorkflowDataSources { AnnotationDataSource = mockDataSource };
        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(asset.ProjectId, false))
                             .ReturnsAsync(mockWorkflowDataSources);
        
        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, mockDataSource.Id))
                        .ReturnsAsync(true);

        var step = CreateStep();

        // Act
        var result = await step.TransferAssetToAnnotationAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockAssetService.Verify(x => x.TransferAssetToDataSourceAsync(asset.AssetId, mockDataSource.Id), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task TransferAssetToAnnotationAsync_WithTransferFailure_ShouldThrowException()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.VETOED);
        var asset = CreateTestAsset(1, 2);
        var currentStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "reviewer123");

        // Mock data source service to return annotation data source
        var mockDataSource = new DataSourceDto { Id = 1, Name = "Test Annotation Source", IsDefault = true, ProjectId = asset.ProjectId, SourceType = DataSourceType.MINIO_BUCKET, Status = DataSourceStatus.ACTIVE, CreatedAt = DateTime.UtcNow, AssetCount = 10 };
        var mockWorkflowDataSources = new WorkflowDataSources { AnnotationDataSource = mockDataSource };
        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(asset.ProjectId, false))
                             .ReturnsAsync(mockWorkflowDataSources);
                             
        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, mockDataSource.Id))
                        .ReturnsAsync(false);

        var step = CreateStep();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            step.TransferAssetToAnnotationAsync(context));
        
        Assert.Contains("Failed to transfer asset back to annotation stage", exception.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldDelegateToTransferAssetAsync()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value))
                        .ReturnsAsync(true);

        var step = CreateStep();

        // Act - using the base IPipelineStep interface method
        var result = await step.ExecuteAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockAssetService.Verify(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithValidContext_ShouldTransferBackToOriginalDataSource()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 2); // Asset now in target data source
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var targetStage = CreateTestWorkflowStage(2, WorkflowStageType.REVISION, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, currentStage.TargetDataSourceId!.Value))
                        .ReturnsAsync(true);

        var step = CreateStep();

        // Act
        var result = await step.RollbackAsync(context);

        // Assert
        Assert.True(result);
        _mockAssetService.Verify(x => x.TransferAssetToDataSourceAsync(asset.AssetId, currentStage.TargetDataSourceId!.Value), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task RollbackAsync_WithTransferFailure_ShouldReturnFalse()
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 2);
        var currentStage = CreateTestWorkflowStage(1, WorkflowStageType.ANNOTATION, 1);
        var context = new PipelineContext(task, asset, currentStage, "user123");

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(false);

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
        Assert.Equal("AssetTransferStep", stepName);
    }

    [Theory]
    [InlineData(WorkflowStageType.ANNOTATION, WorkflowStageType.REVISION)]
    [InlineData(WorkflowStageType.REVISION, WorkflowStageType.COMPLETION)]
    public async System.Threading.Tasks.Task TransferAssetAsync_WithVariousWorkflowStageTransitions_ShouldHandleCorrectly(
        WorkflowStageType fromStageType, WorkflowStageType toStageType)
    {
        // Arrange
        var task = CreateTestTask(1, TaskStatus.IN_PROGRESS);
        var asset = CreateTestAsset(1, 1);
        var currentStage = CreateTestWorkflowStage(1, fromStageType, 1);
        var targetStage = CreateTestWorkflowStage(2, toStageType, 2);
        var context = new PipelineContext(task, asset, currentStage, "user123")
            .WithTargetStage(targetStage);

        _mockAssetService.Setup(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value))
                        .ReturnsAsync(true);

        var step = CreateStep();

        // Act
        var result = await step.TransferAssetAsync(context);

        // Assert
        Assert.NotNull(result);
        _mockAssetService.Verify(x => x.TransferAssetToDataSourceAsync(asset.AssetId, targetStage.TargetDataSourceId!.Value), Times.Once);
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

    private AssetTransferStep CreateStep()
    {
        return new AssetTransferStep(_mockAssetService.Object, _mockDataSourceService.Object, _mockLogger.Object);
    }

    #endregion
}