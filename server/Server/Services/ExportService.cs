using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Models.DTOs.Export;
using server.Models.Domain.Enums;
using server.Services.Interfaces;
using System.Text.Json;

namespace server.Services;

/// <summary>
/// Service for exporting annotated data in various formats
/// </summary>
public class ExportService : IExportService
{
    private readonly LaberisDbContext _context;
    private readonly ILogger<ExportService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ExportService(LaberisDbContext context, ILogger<ExportService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Exports completed tasks from a workflow stage in COCO format
    /// </summary>
    public async Task<byte[]> ExportCocoFormatAsync(int projectId, int workflowStageId, bool includeGroundTruth = true, bool includePredictions = false)
    {
        _logger.LogInformation("Starting COCO export for project {ProjectId}, workflow stage {WorkflowStageId}", projectId, workflowStageId);

        // Get workflow stage info
        var workflowStage = await _context.WorkflowStages
            .Include(ws => ws.Workflow)
            .ThenInclude(w => w.Project)
            .FirstOrDefaultAsync(
                ws => ws.WorkflowStageId == workflowStageId && ws.Workflow.ProjectId == projectId
            ) ?? throw new InvalidOperationException($"Workflow stage {workflowStageId} not found in project {projectId}");

        // Get completed tasks from the workflow stage with their annotations
        _logger.LogDebug("Querying for tasks with: ProjectId={ProjectId}, WorkflowStageId={WorkflowStageId}, IncludeGroundTruth={IncludeGroundTruth}, IncludePredictions={IncludePredictions}", 
            projectId, workflowStageId, includeGroundTruth, includePredictions);

        var completedTasks = await _context.Tasks
            .Where(t => t.WorkflowStageId == workflowStageId && 
                t.ProjectId == projectId && 
                (t.Status == Models.Domain.Enums.TaskStatus.COMPLETED || t.Status == Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION))
            .Include(t => t.Asset)
            .Include(t => t.Annotations
                .Where(a => a.DeletedAt == null && (includeGroundTruth && a.IsGroundTruth || includePredictions && a.IsPrediction)))
                .ThenInclude(a => a.Label)
                .ThenInclude(l => l.LabelScheme)
            .ToListAsync();

        _logger.LogInformation("Found {TaskCount} completed tasks for export", completedTasks.Count);
        
        // Debug task statuses
        var allTasks = await _context.Tasks
            .Where(t => t.WorkflowStageId == workflowStageId && t.ProjectId == projectId)
            .Select(t => new { t.TaskId, t.Status, t.WorkflowStageId, t.ProjectId })
            .ToListAsync();
        _logger.LogDebug("All tasks in workflow stage: {@Tasks}", allTasks);

        // Debug annotations
        var totalAnnotations = completedTasks.SelectMany(t => t.Annotations).Count();
        _logger.LogInformation("Total annotations found: {AnnotationCount}", totalAnnotations);

        // Create COCO dataset
        var cocoDataset = new CocoDatasetDto
        {
            Info = new CocoInfoDto
            {
                Year = DateTime.UtcNow.Year,
                Version = "1.0",
                Description = $"Export from {workflowStage.Workflow.Project.Name} - {workflowStage.Name}",
                Contributor = "Laberis",
                DateCreated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };

        // Get all unique labels (categories)
        var allLabels = completedTasks
            .SelectMany(t => t.Annotations.Select(a => a.Label))
            .DistinctBy(l => l.LabelId)
            .OrderBy(l => l.LabelId)
            .ToList();

        // Create categories
        cocoDataset.Categories = [.. allLabels.Select(label => new CocoCategoryDto
        {
            Id = label.LabelId,
            Name = label.Name,
            SuperCategory = label.LabelScheme.Name,
            Color = label.Color,
            Metadata = string.IsNullOrEmpty(label.Metadata) ? null : 
            JsonSerializer.Deserialize<Dictionary<string, object>>(label.Metadata)
        })];

        // Get all unique assets with annotations
        var assetsWithAnnotations = completedTasks
            .Where(t => t.Annotations.Count != 0)
            .Select(t => t.Asset)
            .DistinctBy(a => a.AssetId)
            .OrderBy(a => a.AssetId)
            .ToList();

        // Create images
        cocoDataset.Images = [.. assetsWithAnnotations.Select(asset => new CocoImageDto
        {
            Id = asset.AssetId,
            Width = asset.Width ?? 0,
            Height = asset.Height ?? 0,
            FileName = asset.Filename,
            DateCaptured = asset.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        })];

        // Create annotations
        var annotationId = 1L;
        foreach (var task in completedTasks.Where(t => t.Annotations.Count != 0))
        {
            foreach (var annotation in task.Annotations)
            {
                var cocoAnnotation = new CocoAnnotationDto
                {
                    Id = annotationId++,
                    ImageId = task.Asset.AssetId,
                    CategoryId = annotation.LabelId
                };

                // Parse annotation data to extract segmentation and bounding box
                try
                {
                    var annotationData = JsonSerializer.Deserialize<JsonElement>(annotation.Data);
                    
                    // Handle different annotation types
                    switch (annotation.AnnotationType)
                    {
                        case AnnotationType.BOUNDING_BOX:
                            var bbox = ExtractBoundingBox(annotationData);
                            cocoAnnotation.BBox = bbox;
                            cocoAnnotation.Area = bbox.Count >= 4 ? bbox[2] * bbox[3] : 0;
                            cocoAnnotation.Segmentation = new List<object>(); // Empty for bounding box
                            break;

                        case AnnotationType.POLYGON:
                            var polygon = ExtractPolygon(annotationData);
                            cocoAnnotation.Segmentation = polygon;
                            cocoAnnotation.Area = CalculatePolygonArea(polygon);
                            cocoAnnotation.BBox = CalculateBoundingBoxFromPolygon(polygon);
                            break;

                        case AnnotationType.POLYLINE:
                            var polyline = ExtractPolyline(annotationData);
                            cocoAnnotation.Segmentation = new List<object>();
                            cocoAnnotation.Area = 0; // Area is zero for polyline // TODO: Should polyline have an area?
                            cocoAnnotation.BBox = CalculateBoundingBoxFromPolyline(polyline);
                            break;

                        case AnnotationType.LINE:
                            var line = ExtractLine(annotationData);
                            cocoAnnotation.Segmentation = new List<object>();
                            cocoAnnotation.Area = 0; // TODO: Should line have an area?
                            cocoAnnotation.BBox = [0, 0, 0, 0];
                            break;

                        case AnnotationType.POINT:
                            var point = ExtractPoint(annotationData);
                            cocoAnnotation.Segmentation = new List<object>();
                            cocoAnnotation.Area = 1; // Single pixel
                            cocoAnnotation.BBox = [point[0], point[1], 1, 1]; // 1x1 bbox around point
                            break;

                        default:
                            // For unsupported types, create empty segmentation
                            cocoAnnotation.Segmentation = new List<object>();
                            cocoAnnotation.Area = 0;
                            cocoAnnotation.BBox = [0, 0, 0, 0];
                            break;
                    }

                    // Add custom attributes
                    cocoAnnotation.Attributes = new Dictionary<string, object>
                    {
                        ["confidence_score"] = annotation.ConfidenceScore ?? 1.0,
                        ["is_ground_truth"] = annotation.IsGroundTruth,
                        ["is_prediction"] = annotation.IsPrediction,
                        ["annotation_type"] = annotation.AnnotationType.ToString(),
                        ["version"] = annotation.Version
                    };

                    if (!string.IsNullOrEmpty(annotation.Notes))
                    {
                        cocoAnnotation.Attributes["notes"] = annotation.Notes;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse annotation data for annotation {AnnotationId}", annotation.AnnotationId);
                    // Create empty annotation
                    cocoAnnotation.Segmentation = new List<object>();
                    cocoAnnotation.Area = 0;
                    cocoAnnotation.BBox = [0, 0, 0, 0];
                }

                cocoDataset.Annotations.Add(cocoAnnotation);
            }
        }

        // Serialize to JSON
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(cocoDataset, _jsonOptions);
        
        _logger.LogInformation("COCO export completed: {ImagesCount} images, {AnnotationsCount} annotations, {CategoriesCount} categories", 
            cocoDataset.Images.Count, cocoDataset.Annotations.Count, cocoDataset.Categories.Count);

        return jsonBytes;
    }

    /// <summary>
    /// Gets export metadata for a workflow stage
    /// </summary>
    public async Task<ExportMetadataDto> GetExportMetadataAsync(int projectId, int workflowStageId)
    {
        var workflowStage = await _context.WorkflowStages
            .Include(ws => ws.Workflow)
                .ThenInclude(w => w.Project)
            .FirstOrDefaultAsync(
                ws => ws.WorkflowStageId == workflowStageId && ws.Workflow.ProjectId == projectId
            ) ?? throw new InvalidOperationException($"Workflow stage {workflowStageId} not found in project {projectId}");

        var completedTasksCount = await _context.Tasks
            .CountAsync(t => t.WorkflowStageId == workflowStageId && 
                t.ProjectId == projectId && 
                (t.Status == Models.Domain.Enums.TaskStatus.COMPLETED || t.Status == Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION));

        var annotationsCount = await _context.Annotations
            .CountAsync(a => a.Task.WorkflowStageId == workflowStageId && 
                a.Task.ProjectId == projectId &&
                (a.Task.Status == Models.Domain.Enums.TaskStatus.COMPLETED || a.Task.Status == Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION) &&
                a.DeletedAt == null);

        var annotatedAssetsCount = await _context.Annotations
            .Where(a => a.Task.WorkflowStageId == workflowStageId && 
                a.Task.ProjectId == projectId &&
                (a.Task.Status == Models.Domain.Enums.TaskStatus.COMPLETED || a.Task.Status == Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION) &&
                a.DeletedAt == null)
            .Select(a => a.AssetId)
            .Distinct()
            .CountAsync();

        var categoriesCount = await _context.Annotations
            .Where(a => a.Task.WorkflowStageId == workflowStageId && 
                a.Task.ProjectId == projectId &&
                (a.Task.Status == Models.Domain.Enums.TaskStatus.COMPLETED || a.Task.Status == Models.Domain.Enums.TaskStatus.READY_FOR_COMPLETION) &&
                a.DeletedAt == null)
            .Select(a => a.LabelId)
            .Distinct()
            .CountAsync();

        return new ExportMetadataDto
        {
            CompletedTasksCount = completedTasksCount,
            AnnotationsCount = annotationsCount,
            AnnotatedAssetsCount = annotatedAssetsCount,
            CategoriesCount = categoriesCount,
            WorkflowStageName = workflowStage.Name,
            ProjectName = workflowStage.Workflow.Project.Name,
            AvailableFormats = ["COCO"]
        };
    }

    #region Private Helper Methods

    /// <summary>
    /// Extracts bounding box from annotation data
    /// Expected format: [x, y, width, height]
    /// </summary>
    private static List<double> ExtractBoundingBox(JsonElement data)
    {
        if (data.TryGetProperty("bbox", out var bboxElement) && bboxElement.ValueKind == JsonValueKind.Array)
        {
            return [.. bboxElement.EnumerateArray().Select(e => e.GetDouble())];
        }

        if (data.TryGetProperty("x", out var xElement) &&
            data.TryGetProperty("y", out var yElement) &&
            data.TryGetProperty("width", out var widthElement) &&
            data.TryGetProperty("height", out var heightElement))
        {
            return [xElement.GetDouble(), yElement.GetDouble(), widthElement.GetDouble(), heightElement.GetDouble()];
        }

        return [0, 0, 0, 0];
    }

    /// <summary>
    /// Extracts line from annotation data
    /// Expected format: array of [x1, y1, x2, y2]
    /// </summary>
    private static List<double> ExtractLine(JsonElement data)
    {
        if (data.TryGetProperty("line", out var lineElement) && lineElement.ValueKind == JsonValueKind.Array)
        {
            return [.. lineElement.EnumerateArray().Select(e => e.GetDouble())];
        }

        return [0, 0, 0, 0];
    }

    /// <summary>
    /// Extracts polyline from annotation data
    /// Expected format: array of [x1, y1, x2, y2, ..., xn, yn]
    /// </summary>
    private static List<double> ExtractPolyline(JsonElement data)
    {
        if (data.TryGetProperty("polyline", out var polylineElement) && polylineElement.ValueKind == JsonValueKind.Array)
        {
            return [.. polylineElement.EnumerateArray().Select(e => e.GetDouble())];
        }

        return [];
    }

    /// <summary>
    /// Extracts polygon from annotation data
    /// Expected format: array of [x1, y1, x2, y2, ..., xn, yn]
    /// </summary>
    private static List<double> ExtractPolygon(JsonElement data)
    {
        if (data.TryGetProperty("points", out var pointsElement) && pointsElement.ValueKind == JsonValueKind.Array)
        {
            var points = new List<double>();
            foreach (var point in pointsElement.EnumerateArray())
            {
                if (point.TryGetProperty("x", out var x) && point.TryGetProperty("y", out var y))
                {
                    points.Add(x.GetDouble());
                    points.Add(y.GetDouble());
                }
            }
            return points;
        }

        if (data.TryGetProperty("polygon", out var polygonElement) && polygonElement.ValueKind == JsonValueKind.Array)
        {
            return [.. polygonElement.EnumerateArray().Select(e => e.GetDouble())];
        }

        return [];
    }

    /// <summary>
    /// Extracts point from annotation data
    /// Expected format: [x, y]
    /// </summary>
    private static List<double> ExtractPoint(JsonElement data)
    {
        if (data.TryGetProperty("x", out var xElement) && data.TryGetProperty("y", out var yElement))
        {
            return [xElement.GetDouble(), yElement.GetDouble()];
        }

        if (data.TryGetProperty("point", out var pointElement) && pointElement.ValueKind == JsonValueKind.Array)
        {
            return [.. pointElement.EnumerateArray().Select(e => e.GetDouble())];
        }

        return [0, 0];
    }

    /// <summary>
    /// Calculates the area of a polygon using the shoelace formula
    /// </summary>
    private static double CalculatePolygonArea(List<double> polygon)
    {
        if (polygon.Count < 6) return 0; // Need at least 3 points (6 coordinates)

        double area = 0;
        int n = polygon.Count / 2;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += polygon[i * 2] * polygon[j * 2 + 1];
            area -= polygon[j * 2] * polygon[i * 2 + 1];
        }

        return Math.Abs(area) / 2.0;
    }

    /// <summary>
    /// Calculates bounding box from polyline points
    /// Returns [x, y, width, height]
    /// </summary>
    private static List<double> CalculateBoundingBoxFromPolyline(List<double> polyline)
    {
        if (polyline.Count < 4) return [0, 0, 0, 0];

        var minX = polyline[0];
        var maxX = polyline[0];
        var minY = polyline[1];
        var maxY = polyline[1];

        for (int i = 2; i < polyline.Count; i += 2)
        {
            minX = Math.Min(minX, polyline[i]);
            maxX = Math.Max(maxX, polyline[i]);
            minY = Math.Min(minY, polyline[i + 1]);
            maxY = Math.Max(maxY, polyline[i + 1]);
        }

        return [minX, minY, maxX - minX, maxY - minY];
    }

    /// <summary>
    /// Calculates bounding box from polygon points
    /// Returns [x, y, width, height]
    /// </summary>
    private static List<double> CalculateBoundingBoxFromPolygon(List<double> polygon)
    {
        if (polygon.Count < 2) return [0, 0, 0, 0];

        var xCoords = new List<double>();
        var yCoords = new List<double>();

        for (int i = 0; i < polygon.Count; i += 2)
        {
            xCoords.Add(polygon[i]);
            if (i + 1 < polygon.Count)
                yCoords.Add(polygon[i + 1]);
        }

        if (xCoords.Count == 0 || yCoords.Count == 0) return [0, 0, 0, 0];

        var minX = xCoords.Min();
        var maxX = xCoords.Max();
        var minY = yCoords.Min();
        var maxY = yCoords.Max();

        return [minX, minY, maxX - minX, maxY - minY];
    }

    #endregion
}