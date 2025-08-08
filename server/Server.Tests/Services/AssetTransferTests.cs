using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Repositories.Interfaces;
using server.Services;
using server.Services.Interfaces;
using Xunit;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace Server.Tests.Services;

public class AssetTransferTests
{
    private readonly Mock<IAssetRepository> _mockAssetRepository;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IDataSourceRepository> _mockDataSourceRepository;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IDomainEventService> _mockDomainEventService;
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<ILogger<AssetService>> _mockLogger;
    private readonly AssetService _assetService;

    public AssetTransferTests()
    {
        _mockAssetRepository = new Mock<IAssetRepository>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockDataSourceRepository = new Mock<IDataSourceRepository>();
        _mockStorageService = new Mock<IStorageService>();
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockDomainEventService = new Mock<IDomainEventService>();
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockLogger = new Mock<ILogger<AssetService>>();

        _assetService = new AssetService(
            _mockAssetRepository.Object,
            _mockFileStorageService.Object,
            _mockDataSourceRepository.Object,
            _mockStorageService.Object,
            _mockTaskRepository.Object,
            _mockWorkflowStageRepository.Object,
            _mockDomainEventService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_TaskCompletion_ShouldTransferAssetAndCreateTasks()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const int currentDataSourceId = 1;
        const int nextDataSourceId = 2;
        const int nextStageId = 5;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.ANNOTATION);
        var asset = CreateTestAsset(assetId, currentDataSourceId);
        var currentDataSource = CreateTestDataSource(currentDataSourceId, "annotation");
        var nextDataSource = CreateTestDataSource(nextDataSourceId, "review");
        var nextStage = CreateTestWorkflowStage(nextStageId, nextDataSourceId);

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync(asset);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(currentDataSourceId))
            .ReturnsAsync(currentDataSource);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(nextDataSourceId))
            .ReturnsAsync(nextDataSource);

        _mockTaskRepository
            .Setup(x => x.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId, It.IsAny<string?>()))
            .ReturnsAsync(nextStage);

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, currentDataSource.Name))
            .Returns("annotation-1");

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, nextDataSource.Name))
            .Returns("review-1");

        _mockStorageService
            .Setup(x => x.BucketExistsAsync("review-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("review-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sourceStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _mockFileStorageService
            .Setup(x => x.DownloadFileAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceStream);

        _mockFileStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), "review-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset.ExternalId);

        _mockAssetRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.True(result.AssetMoved);
        Assert.False(result.ShouldArchiveTask); // Task remains completed, not archived, for veto operations
        Assert.Equal(nextDataSourceId, result.TargetDataSourceId);
        Assert.Equal(nextStageId, result.TargetWorkflowStageId);
        Assert.Null(result.ErrorMessage);

        // Verify asset data source was updated
        Assert.Equal(nextDataSourceId, asset.DataSourceId);

        // Verify all expected calls were made
        _mockFileStorageService.Verify(x => x.DownloadFileAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), "review-1", asset.ExternalId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAssetRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskVetoAssetMovementAsync_ShouldTransferAssetBackToAnnotation()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const int currentDataSourceId = 3; // completion stage
        const int annotationDataSourceId = 1;
        const int annotationStageId = 1;
        const string userId = "manager123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.COMPLETION);
        var asset = CreateTestAsset(assetId, currentDataSourceId);
        var currentDataSource = CreateTestDataSource(currentDataSourceId, "completion");
        var annotationDataSource = CreateTestDataSource(annotationDataSourceId, "annotation");
        var annotationStage = CreateTestWorkflowStage(annotationStageId, annotationDataSourceId);

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync(asset);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(currentDataSourceId))
            .ReturnsAsync(currentDataSource);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(annotationDataSourceId))
            .ReturnsAsync(annotationDataSource);

        _mockTaskRepository
            .Setup(x => x.GetInitialWorkflowStageAsync(task.WorkflowId))
            .ReturnsAsync(annotationStage);

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, currentDataSource.Name))
            .Returns("completion-1");

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, annotationDataSource.Name))
            .Returns("annotation-1");

        _mockStorageService
            .Setup(x => x.BucketExistsAsync("annotation-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("completion-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sourceStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _mockFileStorageService
            .Setup(x => x.DownloadFileAsync("completion-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceStream);

        _mockFileStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), "annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset.ExternalId);

        _mockAssetRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _assetService.HandleTaskVetoAssetMovementAsync(task, userId);

        // Assert
        Assert.True(result.AssetMoved);
        Assert.Equal(annotationDataSourceId, result.TargetDataSourceId);
        Assert.Equal(annotationStageId, result.TargetWorkflowStageId);
        Assert.Null(result.ErrorMessage);

        // Verify asset data source was updated
        Assert.Equal(annotationDataSourceId, asset.DataSourceId);

        // Verify all expected calls were made
        _mockFileStorageService.Verify(x => x.DownloadFileAsync("completion-1", asset.ExternalId, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), "annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAssetRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_AssetAlreadyInTargetDataSource_ShouldSkipTransfer()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const int dataSourceId = 2;
        const int nextStageId = 5;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.ANNOTATION);
        var asset = CreateTestAsset(assetId, dataSourceId);
        var nextStage = CreateTestWorkflowStage(nextStageId, dataSourceId); // Same data source

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync(asset);

        _mockTaskRepository
            .Setup(x => x.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId, It.IsAny<string?>()))
            .ReturnsAsync(nextStage);

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.True(result.AssetMoved);
        Assert.False(result.ShouldArchiveTask); // Task remains completed for veto operations
        Assert.Equal(dataSourceId, result.TargetDataSourceId);
        Assert.Equal(nextStageId, result.TargetWorkflowStageId);
        Assert.Null(result.ErrorMessage);

        // Verify no file operations were performed since asset is already in target data source
        _mockFileStorageService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_FileAlreadyExistsInTarget_ShouldSkipCopy()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const int currentDataSourceId = 1;
        const int nextDataSourceId = 2;
        const int nextStageId = 5;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.ANNOTATION);
        var asset = CreateTestAsset(assetId, currentDataSourceId);
        var currentDataSource = CreateTestDataSource(currentDataSourceId, "annotation");
        var nextDataSource = CreateTestDataSource(nextDataSourceId, "review");
        var nextStage = CreateTestWorkflowStage(nextStageId, nextDataSourceId);

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync(asset);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(currentDataSourceId))
            .ReturnsAsync(currentDataSource);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(nextDataSourceId))
            .ReturnsAsync(nextDataSource);

        _mockTaskRepository
            .Setup(x => x.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId, It.IsAny<string?>()))
            .ReturnsAsync(nextStage);

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, currentDataSource.Name))
            .Returns("annotation-1");

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, nextDataSource.Name))
            .Returns("review-1");

        _mockStorageService
            .Setup(x => x.BucketExistsAsync("review-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("review-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // File already exists

        _mockAssetRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.True(result.AssetMoved);
        Assert.False(result.ShouldArchiveTask); // Task remains completed for veto operations
        Assert.Equal(nextDataSourceId, result.TargetDataSourceId);
        Assert.Equal(nextStageId, result.TargetWorkflowStageId);
        Assert.Null(result.ErrorMessage);

        // Verify asset data source was updated
        Assert.Equal(nextDataSourceId, asset.DataSourceId);

        // Verify file was not downloaded/uploaded since it already exists
        _mockFileStorageService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAssetRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_SourceFileNotFound_ShouldReturnError()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const int currentDataSourceId = 1;
        const int nextDataSourceId = 2;
        const int nextStageId = 5;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.ANNOTATION);
        var asset = CreateTestAsset(assetId, currentDataSourceId);
        var currentDataSource = CreateTestDataSource(currentDataSourceId, "annotation");
        var nextDataSource = CreateTestDataSource(nextDataSourceId, "review");
        var nextStage = CreateTestWorkflowStage(nextStageId, nextDataSourceId);

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync(asset);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(currentDataSourceId))
            .ReturnsAsync(currentDataSource);

        _mockDataSourceRepository
            .Setup(x => x.GetByIdAsync(nextDataSourceId))
            .ReturnsAsync(nextDataSource);

        _mockTaskRepository
            .Setup(x => x.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId, It.IsAny<string?>()))
            .ReturnsAsync(nextStage);

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, currentDataSource.Name))
            .Returns("annotation-1");

        _mockStorageService
            .Setup(x => x.GenerateBucketName(task.ProjectId, nextDataSource.Name))
            .Returns("review-1");

        _mockStorageService
            .Setup(x => x.BucketExistsAsync("review-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockFileStorageService
            .Setup(x => x.FileExistsAsync("annotation-1", asset.ExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Source file does not exist

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.False(result.AssetMoved);
        Assert.True(result.ShouldArchiveTask);
        Assert.Null(result.TargetDataSourceId);
        Assert.Null(result.TargetWorkflowStageId);
        Assert.Equal("Failed to transfer asset to next workflow stage", result.ErrorMessage);

        // Verify asset data source was not updated
        Assert.Equal(currentDataSourceId, asset.DataSourceId);

        // Verify no file operations beyond existence check
        _mockFileStorageService.Verify(x => x.DownloadFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAssetRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_AssetNotFound_ShouldReturnError()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.ANNOTATION);
        var nextStage = CreateTestWorkflowStage(5, 2);

        _mockAssetRepository
            .Setup(x => x.GetByIdAsync(assetId))
            .ReturnsAsync((Asset?)null);

        _mockTaskRepository
            .Setup(x => x.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId, It.IsAny<string?>()))
            .ReturnsAsync(nextStage);

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.False(result.AssetMoved);
        Assert.True(result.ShouldArchiveTask);
        Assert.Null(result.TargetDataSourceId);
        Assert.Null(result.TargetWorkflowStageId);
        Assert.Equal("Asset not found for task", result.ErrorMessage);

        // Verify no further operations were performed
        _mockDataSourceRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockFileStorageService.Verify(x => x.FileExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task HandleTaskWorkflowAssetMovementAsync_CompletionStage_ShouldNotMoveAsset()
    {
        // Arrange
        const int taskId = 1;
        const int assetId = 10;
        const string userId = "user123";

        var task = CreateTestTask(taskId, assetId, WorkflowStageType.COMPLETION);

        // Act
        var result = await _assetService.HandleTaskWorkflowAssetMovementAsync(task, TaskStatus.COMPLETED, userId);

        // Assert
        Assert.False(result.AssetMoved);
        Assert.False(result.ShouldArchiveTask);
        Assert.Null(result.TargetDataSourceId);
        Assert.Null(result.TargetWorkflowStageId);
        Assert.Null(result.ErrorMessage);

        // Verify no operations were performed for completion stage
        _mockAssetRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockTaskRepository.Verify(x => x.GetNextWorkflowStageAsync(It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    #region Helper Methods

    private static LaberisTask CreateTestTask(int taskId, int assetId, WorkflowStageType stageType)
    {
        return new LaberisTask
        {
            TaskId = taskId,
            AssetId = assetId,
            ProjectId = 1,
            WorkflowId = 1,
            CurrentWorkflowStageId = 1,
            Priority = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            CurrentWorkflowStage = new WorkflowStage
            {
                WorkflowStageId = 1,
                Name = $"{stageType} Stage",
                StageType = stageType,
                WorkflowId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    private static Asset CreateTestAsset(int assetId, int dataSourceId)
    {
        return new Asset
        {
            AssetId = assetId,
            DataSourceId = dataSourceId,
            ProjectId = 1,
            ExternalId = $"test-asset-{assetId}.jpg",
            Filename = $"test-asset-{assetId}.jpg",
            MimeType = "image/jpeg",
            SizeBytes = 1024,
            Width = 800,
            Height = 600,
            Status = AssetStatus.IMPORTED,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    private static DataSource CreateTestDataSource(int dataSourceId, string name)
    {
        return new DataSource
        {
            DataSourceId = dataSourceId,
            Name = name,
            Description = $"{name} data source",
            SourceType = DataSourceType.MINIO_BUCKET,
            Status = DataSourceStatus.ACTIVE,
            ProjectId = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static WorkflowStage CreateTestWorkflowStage(int stageId, int? inputDataSourceId, WorkflowStageType stageType = WorkflowStageType.REVISION)
    {
        return new WorkflowStage
        {
            WorkflowStageId = stageId,
            Name = $"Stage {stageId}",
            StageType = stageType,
            WorkflowId = 1,
            InputDataSourceId = inputDataSourceId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    #endregion
}