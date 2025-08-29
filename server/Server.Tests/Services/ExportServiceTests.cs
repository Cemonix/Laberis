using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using server.Data;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.Export;
using server.Services;
using Server.Tests.Factories;
using System.Text.Json;
using LaberisTask = server.Models.Domain.Task;
using TaskAsync = System.Threading.Tasks.Task;

namespace Server.Tests.Services;

public class ExportServiceTests
{
    private readonly Mock<ILogger<ExportService>> _mockLogger;

    public ExportServiceTests()
    {
        _mockLogger = new Mock<ILogger<ExportService>>();
    }

    [Fact]
    public async TaskAsync GetExportMetadataAsync_WithValidStage_ReturnsCorrectMetadata()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);
        
        var (projectId, workflowStageId) = await SetupTestDataAsync(context);

        // Act
        var result = await exportService.GetExportMetadataAsync(projectId, workflowStageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Project", result.ProjectName);
        Assert.Equal("Completion Stage", result.WorkflowStageName);
        Assert.Equal(2, result.CompletedTasksCount);
        Assert.Equal(3, result.AnnotationsCount); // 2 + 1 annotations
        Assert.Equal(2, result.AnnotatedAssetsCount); // 2 unique assets
        Assert.Equal(2, result.CategoriesCount); // 2 unique labels
        Assert.Contains("COCO", result.AvailableFormats);
    }

    [Fact]
    public async TaskAsync GetExportMetadataAsync_WithNonExistentStage_ThrowsInvalidOperationException()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => exportService.GetExportMetadataAsync(1, 999));
    }

    [Fact]
    public async TaskAsync ExportCocoFormatAsync_WithCompletedTasks_ReturnsValidCocoData()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);
        
        var (projectId, workflowStageId) = await SetupTestDataAsync(context);

        // Act
        var result = await exportService.ExportCocoFormatAsync(projectId, workflowStageId, true, false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Parse the JSON to verify structure
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        var cocoData = JsonSerializer.Deserialize<CocoDatasetDto>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        Assert.NotNull(cocoData);
        Assert.NotNull(cocoData.Info);
        Assert.Equal("Export from Test Project - Completion Stage", cocoData.Info.Description);
        Assert.Equal(2, cocoData.Images.Count);
        Assert.Equal(3, cocoData.Annotations.Count);
        Assert.Equal(2, cocoData.Categories.Count);

        // Verify image data
        var firstImage = cocoData.Images.First();
        Assert.Equal("test-asset-1.jpg", firstImage.FileName);
        Assert.Equal(800, firstImage.Width);
        Assert.Equal(600, firstImage.Height);

        // Verify annotation data
        var firstAnnotation = cocoData.Annotations.First();
        Assert.True(firstAnnotation.Id > 0);
        Assert.True(firstAnnotation.ImageId > 0);
        Assert.True(firstAnnotation.CategoryId > 0);
        Assert.NotNull(firstAnnotation.Attributes);
        Assert.True((bool)firstAnnotation.Attributes["is_ground_truth"]);

        // Verify category data
        var firstCategory = cocoData.Categories.First();
        Assert.Equal("Test Label 1", firstCategory.Name);
        Assert.Equal("Test Label Scheme", firstCategory.SuperCategory);
    }

    [Fact]
    public async TaskAsync ExportCocoFormatAsync_WithIncludePredictions_IncludesPredictionAnnotations()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);
        
        var (projectId, workflowStageId) = await SetupTestDataWithPredictionsAsync(context);

        // Act - Include predictions
        var result = await exportService.ExportCocoFormatAsync(projectId, workflowStageId, true, true);

        // Parse and verify
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        var cocoData = JsonSerializer.Deserialize<CocoDatasetDto>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Should include both ground truth and prediction annotations
        Assert.NotNull(cocoData);
        Assert.Equal(4, cocoData.Annotations.Count); // 3 ground truth + 1 prediction

        // Verify prediction annotation exists
        var predictionAnnotations = cocoData.Annotations
            .Where(a => a.Attributes != null && a.Attributes.ContainsKey("is_prediction") && (bool)a.Attributes["is_prediction"])
            .ToList();
        Assert.Single(predictionAnnotations);
    }

    [Fact]
    public async TaskAsync ExportCocoFormatAsync_WithExcludePredictions_OnlyIncludesGroundTruth()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);
        
        var (projectId, workflowStageId) = await SetupTestDataWithPredictionsAsync(context);

        // Act - Exclude predictions
        var result = await exportService.ExportCocoFormatAsync(projectId, workflowStageId, true, false);

        // Parse and verify
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        var cocoData = JsonSerializer.Deserialize<CocoDatasetDto>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Should only include ground truth annotations
        Assert.NotNull(cocoData);
        Assert.Equal(3, cocoData.Annotations.Count); // Only ground truth

        // Verify no prediction annotations
        var predictionAnnotations = cocoData.Annotations
            .Where(a => a.Attributes != null && a.Attributes.ContainsKey("is_prediction") && (bool)a.Attributes["is_prediction"])
            .ToList();
        Assert.Empty(predictionAnnotations);
    }

    [Fact]
    public async TaskAsync ExportCocoFormatAsync_WithOnlyInProgressTasks_ReturnsEmpty()
    {
        // Arrange
        using var factory = new DbContextFactory();
        var context = factory.Context;
        var exportService = new ExportService(context, _mockLogger.Object);
        
        var (projectId, workflowStageId) = await SetupTestDataWithInProgressTasksAsync(context);

        // Act
        var result = await exportService.ExportCocoFormatAsync(projectId, workflowStageId, true, false);

        // Parse and verify
        var jsonString = System.Text.Encoding.UTF8.GetString(result);
        var cocoData = JsonSerializer.Deserialize<CocoDatasetDto>(jsonString, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        // Should be empty since no completed tasks
        Assert.NotNull(cocoData);
        Assert.Empty(cocoData.Images);
        Assert.Empty(cocoData.Annotations);
        Assert.Empty(cocoData.Categories);
    }

    private async Task<(int projectId, int workflowStageId)> SetupTestDataAsync(LaberisDbContext context)
    {
        // Create project
        var project = new Project
        {
            Name = "Test Project",
            Description = "Test project for export",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        // Create label scheme
        var labelScheme = new LabelScheme
        {
            Name = "Test Label Scheme",
            Description = "Test labels",
            ProjectId = project.ProjectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.LabelSchemes.Add(labelScheme);
        await context.SaveChangesAsync();

        // Create labels
        var label1 = new Label
        {
            Name = "Test Label 1",
            Color = "#FF0000",
            LabelSchemeId = labelScheme.LabelSchemeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var label2 = new Label
        {
            Name = "Test Label 2",
            Color = "#00FF00",
            LabelSchemeId = labelScheme.LabelSchemeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Labels.AddRange(label1, label2);
        await context.SaveChangesAsync();

        // Create data source
        var dataSource = new DataSource
        {
            Name = "Test Data Source",
            ProjectId = project.ProjectId,
            SourceType = DataSourceType.S3_BUCKET,
            Status = DataSourceStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.DataSources.Add(dataSource);
        await context.SaveChangesAsync();

        // Create workflow
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            ProjectId = project.ProjectId,
            LabelSchemeId = labelScheme.LabelSchemeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        // Create workflow stage (COMPLETION type)
        var workflowStage = new WorkflowStage
        {
            Name = "Completion Stage",
            WorkflowId = workflow.WorkflowId,
            StageType = WorkflowStageType.COMPLETION,
            StageOrder = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.WorkflowStages.Add(workflowStage);
        await context.SaveChangesAsync();

        // Create assets
        var asset1 = new Asset
        {
            ExternalId = "asset-1",
            Filename = "test-asset-1.jpg",
            Width = 800,
            Height = 600,
            MimeType = "image/jpeg",
            ProjectId = project.ProjectId,
            DataSourceId = dataSource.DataSourceId,
            Status = AssetStatus.IMPORTED,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var asset2 = new Asset
        {
            ExternalId = "asset-2",
            Filename = "test-asset-2.jpg",
            Width = 1024,
            Height = 768,
            MimeType = "image/jpeg",
            ProjectId = project.ProjectId,
            DataSourceId = dataSource.DataSourceId,
            Status = AssetStatus.IMPORTED,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Assets.AddRange(asset1, asset2);
        await context.SaveChangesAsync();

        // Create completed tasks
        var task1 = new LaberisTask
        {
            AssetId = asset1.AssetId,
            ProjectId = project.ProjectId,
            WorkflowId = workflow.WorkflowId,
            WorkflowStageId = workflowStage.WorkflowStageId,
            Status = server.Models.Domain.Enums.TaskStatus.COMPLETED,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var task2 = new LaberisTask
        {
            AssetId = asset2.AssetId,
            ProjectId = project.ProjectId,
            WorkflowId = workflow.WorkflowId,
            WorkflowStageId = workflowStage.WorkflowStageId,
            Status = server.Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Tasks.AddRange(task1, task2);
        await context.SaveChangesAsync();

        // Create annotations
        var annotation1 = new Annotation
        {
            TaskId = task1.TaskId,
            AssetId = asset1.AssetId,
            LabelId = label1.LabelId,
            AnnotationType = AnnotationType.BOUNDING_BOX,
            Data = JsonSerializer.Serialize(new { x = 10, y = 20, width = 100, height = 80 }),
            IsGroundTruth = true,
            AnnotatorUserId = "test-user",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var annotation2 = new Annotation
        {
            TaskId = task1.TaskId,
            AssetId = asset1.AssetId,
            LabelId = label2.LabelId,
            AnnotationType = AnnotationType.POLYGON,
            Data = JsonSerializer.Serialize(new { points = new[] { new { x = 50, y = 60 }, new { x = 150, y = 60 }, new { x = 100, y = 140 } } }),
            IsGroundTruth = true,
            AnnotatorUserId = "test-user",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var annotation3 = new Annotation
        {
            TaskId = task2.TaskId,
            AssetId = asset2.AssetId,
            LabelId = label1.LabelId,
            AnnotationType = AnnotationType.POINT,
            Data = JsonSerializer.Serialize(new { x = 200, y = 300 }),
            IsGroundTruth = true,
            AnnotatorUserId = "test-user",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Annotations.AddRange(annotation1, annotation2, annotation3);
        await context.SaveChangesAsync();

        return (project.ProjectId, workflowStage.WorkflowStageId);
    }

    private async System.Threading.Tasks.Task<(int projectId, int workflowStageId)> SetupTestDataWithPredictionsAsync(LaberisDbContext context)
    {
        var (projectId, workflowStageId) = await SetupTestDataAsync(context);

        // Add prediction annotation
        var task = await context.Tasks.FirstAsync();
        var asset = await context.Assets.FirstAsync();
        var label = await context.Labels.FirstAsync();

        var predictionAnnotation = new Annotation
        {
            TaskId = task.TaskId,
            AssetId = asset.AssetId,
            LabelId = label.LabelId,
            AnnotationType = AnnotationType.BOUNDING_BOX,
            Data = JsonSerializer.Serialize(new { x = 50, y = 50, width = 80, height = 60 }),
            IsPrediction = true,
            IsGroundTruth = false,
            ConfidenceScore = 0.85,
            AnnotatorUserId = "ai-model",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Annotations.Add(predictionAnnotation);
        await context.SaveChangesAsync();

        return (projectId, workflowStageId);
    }

    private async Task<(int projectId, int workflowStageId)> SetupTestDataWithInProgressTasksAsync(LaberisDbContext context)
    {
        var (projectId, workflowStageId) = await SetupTestDataAsync(context);

        // Update all tasks to be IN_PROGRESS instead of COMPLETED
        var tasks = await context.Tasks.ToListAsync();
        foreach (var task in tasks)
        {
            task.Status = server.Models.Domain.Enums.TaskStatus.IN_PROGRESS;
            task.CompletedAt = null;
        }
        await context.SaveChangesAsync();

        return (projectId, workflowStageId);
    }
}