using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.WorkflowStage;
using server.Models.DTOs.DataSource;
using server.Models.Internal;
using server.Repositories.Interfaces;
using server.Services;
using server.Services.Interfaces;
using Server.Tests.Factories;

namespace Server.Tests.Services;

/// <summary>
/// Tests for WorkflowStageService focusing on the workflow stage creation pipeline.
/// These tests help identify issues with workflow stage creation that causes empty workflow stages table.
/// </summary>
public class WorkflowStageServiceTests : IDisposable
{
    private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
    private readonly Mock<IDataSourceService> _mockDataSourceService;
    private readonly Mock<ILogger<WorkflowStageService>> _mockLogger;
    private readonly WorkflowStageService _workflowStageService;
    private readonly DbContextFactory _dbContextFactory;

    public WorkflowStageServiceTests()
    {
        _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
        _mockDataSourceService = new Mock<IDataSourceService>();
        _mockLogger = new Mock<ILogger<WorkflowStageService>>();
        _dbContextFactory = new DbContextFactory();

        _workflowStageService = new WorkflowStageService(
            _mockWorkflowStageRepository.Object,
            _mockDataSourceService.Object,
            _mockLogger.Object
        );
    }

    public void Dispose()
    {
        _dbContextFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStagesPipelineAsync_ShouldReturnEmpty_WhenAnnotationDataSourceIsNull()
    {
        // Arrange
        int workflowId = 1;
        int projectId = 1;
        bool includeReviewStage = false;

        var workflowDataSources = new WorkflowDataSources
        {
            AnnotationDataSource = null, // This should cause the method to return early
            CompletionDataSource = new DataSourceDto { Id = 3, Name = "Completion" }
        };

        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage))
            .ReturnsAsync(workflowDataSources);

        // Act
        var result = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
            workflowId, projectId, includeReviewStage);

        // Assert
        Assert.Null(result.initialStageId);
        Assert.Empty(result.createdStages);

        // Verify the data source service was called
        _mockDataSourceService.Verify(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStagesPipelineAsync_ShouldReturnEmpty_WhenReviewDataSourceRequiredButNull()
    {
        // Arrange
        int workflowId = 1;
        int projectId = 1;
        bool includeReviewStage = true; // Review stage is requested

        var workflowDataSources = new WorkflowDataSources
        {
            AnnotationDataSource = new DataSourceDto { Id = 1, Name = "Annotation" },
            ReviewDataSource = null, // Review stage requested but data source is null
            CompletionDataSource = new DataSourceDto { Id = 3, Name = "Completion" }
        };

        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage))
            .ReturnsAsync(workflowDataSources);

        // Act
        var result = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
            workflowId, projectId, includeReviewStage);

        // Assert
        Assert.Null(result.initialStageId);
        Assert.Empty(result.createdStages);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStagesPipelineAsync_ShouldReturnEmpty_WhenCompletionDataSourceIsNull()
    {
        // Arrange
        int workflowId = 1;
        int projectId = 1;
        bool includeReviewStage = false;

        var workflowDataSources = new WorkflowDataSources
        {
            AnnotationDataSource = new DataSourceDto { Id = 1, Name = "Annotation" },
            CompletionDataSource = null // This should cause the method to return early
        };

        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage))
            .ReturnsAsync(workflowDataSources);

        // Act
        var result = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
            workflowId, projectId, includeReviewStage);

        // Assert
        Assert.Null(result.initialStageId);
        Assert.Empty(result.createdStages);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStagesPipelineAsync_ShouldCreateTwoStages_WhenNoReviewStageRequested()
    {
        // Arrange
        int workflowId = 1;
        int projectId = 1;
        bool includeReviewStage = false;

        var workflowDataSources = new WorkflowDataSources
        {
            AnnotationDataSource = new DataSourceDto { Id = 1, Name = "Annotation" },
            CompletionDataSource = new DataSourceDto { Id = 3, Name = "Completion" }
        };

        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage))
            .ReturnsAsync(workflowDataSources);

        // Mock the CreateWorkflowStageAsync calls
        _mockWorkflowStageRepository.Setup(x => x.AddAsync(It.IsAny<WorkflowStage>()))
            .Callback<WorkflowStage>(stage => stage.WorkflowStageId = 1); // Simulate ID assignment

        _mockWorkflowStageRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Setup stage creation sequence
        var annotationStage = new WorkflowStageDto
        {
            Id = 1,
            Name = "Annotation",
            StageType = WorkflowStageType.ANNOTATION,
            IsInitialStage = true,
            IsFinalStage = true, // No review stage, so annotation is final
            InputDataSourceId = 1,
            TargetDataSourceId = 3
        };

        var completionStage = new WorkflowStageDto
        {
            Id = 2,
            Name = "Completion",
            StageType = WorkflowStageType.COMPLETION,
            IsInitialStage = false,
            IsFinalStage = true,
            InputDataSourceId = 3,
            TargetDataSourceId = null
        };

        // Use the actual CreateWorkflowStageAsync method from WorkflowStageService
        // We need to mock the repository calls that this method makes
        var callCount = 0;
        _mockWorkflowStageRepository.Setup(x => x.AddAsync(It.IsAny<WorkflowStage>()))
            .Callback<WorkflowStage>(stage =>
            {
                callCount++;
                stage.WorkflowStageId = callCount; // Assign sequential IDs
            });

        // Act
        var (initialStageId, createdStages) = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
            workflowId, projectId, includeReviewStage);

        // Assert
        Assert.NotNull(initialStageId);
        Assert.Equal(2, createdStages.Count);
        
        var createdAnnotationStage = createdStages.First(s => s.StageType == WorkflowStageType.ANNOTATION);
        var createdCompletionStage = createdStages.First(s => s.StageType == WorkflowStageType.COMPLETION);
        
        Assert.Equal("Annotation", createdAnnotationStage.Name);
        Assert.True(createdAnnotationStage.IsInitialStage);
        Assert.False(createdAnnotationStage.IsFinalStage); // Should be false - completion stage is final
        Assert.Equal(1, createdAnnotationStage.InputDataSourceId);
        Assert.Equal(3, createdAnnotationStage.TargetDataSourceId);

        Assert.Equal("Completion", createdCompletionStage.Name);
        Assert.False(createdCompletionStage.IsInitialStage);
        Assert.True(createdCompletionStage.IsFinalStage);
        Assert.Equal(3, createdCompletionStage.InputDataSourceId);
        Assert.Null(createdCompletionStage.TargetDataSourceId);

        // Verify data source service was called
        _mockDataSourceService.Verify(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage), Times.Once);

        // Verify that AddAsync was called twice (annotation + completion)
        _mockWorkflowStageRepository.Verify(x => x.AddAsync(It.IsAny<WorkflowStage>()), Times.Exactly(2));
        _mockWorkflowStageRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStagesPipelineAsync_ShouldCreateThreeStages_WhenReviewStageRequested()
    {
        // Arrange
        int workflowId = 1;
        int projectId = 1;
        bool includeReviewStage = true;

        var workflowDataSources = new WorkflowDataSources
        {
            AnnotationDataSource = new DataSourceDto { Id = 1, Name = "Annotation" },
            ReviewDataSource = new DataSourceDto { Id = 2, Name = "Review" },
            CompletionDataSource = new DataSourceDto { Id = 3, Name = "Completion" }
        };

        _mockDataSourceService.Setup(x => x.EnsureRequiredDataSourcesExistAsync(projectId, includeReviewStage))
            .ReturnsAsync(workflowDataSources);

        var callCount = 0;
        _mockWorkflowStageRepository.Setup(x => x.AddAsync(It.IsAny<WorkflowStage>()))
            .Callback<WorkflowStage>(stage =>
            {
                callCount++;
                stage.WorkflowStageId = callCount;
            });

        _mockWorkflowStageRepository.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _workflowStageService.CreateWorkflowStagesPipelineAsync(
            workflowId, projectId, includeReviewStage);

        // Assert
        Assert.NotNull(result.initialStageId);
        Assert.Equal(3, result.createdStages.Count);

        var annotationStage = result.createdStages.First(s => s.StageType == WorkflowStageType.ANNOTATION);
        var reviewStage = result.createdStages.First(s => s.StageType == WorkflowStageType.REVISION);
        var completionStage = result.createdStages.First(s => s.StageType == WorkflowStageType.COMPLETION);

        // Verify annotation stage
        Assert.Equal("Annotation", annotationStage.Name);
        Assert.True(annotationStage.IsInitialStage);
        Assert.False(annotationStage.IsFinalStage);
        Assert.Equal(1, annotationStage.InputDataSourceId);
        Assert.Equal(2, annotationStage.TargetDataSourceId); // Points to review

        // Verify review stage
        Assert.Equal("Review", reviewStage.Name);
        Assert.False(reviewStage.IsInitialStage);
        Assert.False(reviewStage.IsFinalStage);
        Assert.Equal(2, reviewStage.InputDataSourceId);
        Assert.Equal(3, reviewStage.TargetDataSourceId); // Points to completion

        // Verify completion stage
        Assert.Equal("Completion", completionStage.Name);
        Assert.False(completionStage.IsInitialStage);
        Assert.True(completionStage.IsFinalStage);
        Assert.Equal(3, completionStage.InputDataSourceId);
        Assert.Null(completionStage.TargetDataSourceId);

        // Verify all stages were created
        _mockWorkflowStageRepository.Verify(x => x.AddAsync(It.IsAny<WorkflowStage>()), Times.Exactly(3));
        _mockWorkflowStageRepository.Verify(x => x.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateWorkflowStageAsync_ShouldThrowException_WhenDataSourceValidationFails()
    {
        // Arrange
        int workflowId = 1;
        var createDto = new CreateWorkflowStageDto
        {
            Name = "Test Stage",
            StageType = WorkflowStageType.ANNOTATION,
            InputDataSourceId = 1,
            TargetDataSourceId = 2
        };

        // Mock conflicting data source usage
        var conflictingStages = new List<WorkflowStage>
        {
            new()
            {
                WorkflowStageId = 2,
                Name = "Conflicting Stage",
                Workflow = new Workflow { WorkflowId = 2, Name = "Other Workflow" }
            }
        };

        _mockWorkflowStageRepository.Setup(x => x.GetConflictingDataSourceUsageAsync(1, workflowId))
            .ReturnsAsync(conflictingStages);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workflowStageService.CreateWorkflowStageAsync(workflowId, createDto));

        Assert.Contains("Data source 1 cannot be used", exception.Message);
        Assert.Contains("already in use by other workflow stages", exception.Message);
    }
}