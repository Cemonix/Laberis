using server.Models.Domain;
using server.Models.DTOs.Asset;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;
using server.Models.Domain.Enums;
using server.Utils;
using server.Exceptions;
using server.Services.Storage;
using LaberisTask = server.Models.Domain.Task;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace server.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IStorageService _storageService;
    private readonly ITaskService _taskService;
    private readonly ITaskRepository _taskRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly ILogger<AssetService> _logger;

    public AssetService(
        IAssetRepository assetRepository,
        IFileStorageService fileStorageService,
        IDataSourceRepository dataSourceRepository,
        IStorageService storageService,
        ITaskService taskService,
        ITaskRepository taskRepository,
        IWorkflowStageRepository workflowStageRepository,
        ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository ?? throw new ArgumentNullException(nameof(assetRepository));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _dataSourceRepository = dataSourceRepository ?? throw new ArgumentNullException(nameof(dataSourceRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<AssetDto>> GetAssetsForProjectAsync(
        int projectId,
        int? dataSourceId = null,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25,
        bool includeUrls = true, int urlExpiryInSeconds = 3600)
    {
        _logger.LogInformation("Fetching assets for project: {ProjectId}, dataSource: {DataSourceId}", projectId, dataSourceId);

        var (assets, totalCount) = await _assetRepository.GetAllWithCountAsync(
            filter: a => a.ProjectId == projectId && (dataSourceId == null || a.DataSourceId == dataSourceId),
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} assets for project: {ProjectId}", assets.Count(), projectId);

        var assetDtos = new List<AssetDto>();
        
        foreach (var asset in assets)
        {
            string? imageUrl = null;

            if (includeUrls)
            {
                try
                {
                    // Get the data source to determine the bucket name
                    var dataSource = await _dataSourceRepository.GetByIdAsync(asset.DataSourceId);
                    if (dataSource != null)
                    {
                        var bucketName = _storageService.GenerateBucketName(asset.ProjectId, dataSource.Name);
                        imageUrl = await _storageService.GetPresignedUrlAsync(bucketName, asset.ExternalId, urlExpiryInSeconds);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate URL for asset {AssetId}", asset.AssetId);
                    // Continue without URL rather than failing the entire request
                }
            }

            assetDtos.Add(new AssetDto
            {
                Id = asset.AssetId,
                Filename = asset.Filename,
                MimeType = asset.MimeType,
                SizeBytes = asset.SizeBytes,
                Width = asset.Width,
                Height = asset.Height,
                DurationMs = asset.DurationMs,
                Status = asset.Status,
                CreatedAt = asset.CreatedAt,
                UpdatedAt = asset.UpdatedAt,
                ImageUrl = imageUrl
            });
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<AssetDto>
        {
            Data = assetDtos.ToArray(),
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<AssetDto?> GetAssetByIdAsync(int assetId)
    {
        _logger.LogInformation("Fetching asset with ID: {AssetId}", assetId);
        
        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found.", assetId);
            return null;
        }

        // Generate presigned URL for the asset
        string? imageUrl = null;
        try
        {
            // Get the data source to determine the bucket name
            var dataSource = await _dataSourceRepository.GetByIdAsync(asset.DataSourceId);
            if (dataSource != null)
            {
                var bucketName = _storageService.GenerateBucketName(asset.ProjectId, dataSource.Name);
                imageUrl = await _storageService.GetPresignedUrlAsync(bucketName, asset.ExternalId, 3600); // 1 hour expiry
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate URL for asset {AssetId}", assetId);
            // Continue without URL rather than failing the entire request
        }

        return MapToDto(asset, imageUrl);
    }

    public async Task<AssetDto> CreateAssetAsync(int projectId, CreateAssetDto createDto)
    {
        _logger.LogInformation("Creating new asset for project: {ProjectId}", projectId);

        var asset = new Asset
        {
            ExternalId = createDto.ExternalId,
            Filename = createDto.Filename,
            MimeType = createDto.MimeType,
            SizeBytes = createDto.SizeBytes,
            Width = createDto.Width,
            Height = createDto.Height,
            DurationMs = createDto.DurationMs,
            Metadata = createDto.Metadata,
            ProjectId = projectId,
            DataSourceId = createDto.DataSourceId,
            Status = AssetStatus.PENDING_IMPORT,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _assetRepository.AddAsync(asset);
        await _assetRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created asset with ID: {AssetId}", asset.AssetId);
        
        return MapToDto(asset);
    }

    public async Task<AssetDto?> UpdateAssetAsync(int assetId, UpdateAssetDto updateDto)
    {
        _logger.LogInformation("Updating asset with ID: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found for update.", assetId);
            return null;
        }

        asset.Filename = updateDto.Filename ?? asset.Filename;
        asset.MimeType = updateDto.MimeType ?? asset.MimeType;
        asset.SizeBytes = updateDto.SizeBytes ?? asset.SizeBytes;
        asset.Width = updateDto.Width ?? asset.Width;
        asset.Height = updateDto.Height ?? asset.Height;
        asset.DurationMs = updateDto.DurationMs ?? asset.DurationMs;
        asset.Metadata = updateDto.Metadata ?? asset.Metadata;
        // Auto-create tasks if asset was just marked as IMPORTED
        bool wasJustImported = updateDto.Status == AssetStatus.IMPORTED && asset.Status != AssetStatus.IMPORTED;
        
        asset.Status = updateDto.Status;
        asset.DataSourceId = updateDto.DataSourceId ?? asset.DataSourceId;
        asset.UpdatedAt = DateTime.UtcNow;

        await _assetRepository.SaveChangesAsync();

        if (wasJustImported)
        {
            await TryCreateTasksForNewAssetAsync(asset);
        }

        _logger.LogInformation("Successfully updated asset with ID: {AssetId}", assetId);
        
        return MapToDto(asset);
    }

    public async Task<bool> DeleteAssetAsync(int assetId)
    {
        _logger.LogInformation("Deleting asset with ID: {AssetId}", assetId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found for deletion.", assetId);
            return false;
        }

        _assetRepository.Remove(asset);
        await _assetRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted asset with ID: {AssetId}", assetId);
        
        return true;
    }

    /// <summary>
    /// Attempts to create tasks for a newly imported asset across all active workflows.
    /// </summary>
    /// <param name="asset">The asset that was just imported.</param>
    private async System.Threading.Tasks.Task TryCreateTasksForNewAssetAsync(Asset asset)
    {
        try
        {
            _logger.LogInformation("Attempting to create tasks for newly imported asset {AssetId} in data source {DataSourceId}", 
                asset.AssetId, asset.DataSourceId);

            // Find all workflow stages where InputDataSourceId == asset.DataSourceId
            var workflowStages = await _workflowStageRepository.FindAsync(
                ws => ws.InputDataSourceId == asset.DataSourceId && 
                      ws.Workflow.ProjectId == asset.ProjectId);

            var relevantStages = workflowStages.ToList();
            
            if (!relevantStages.Any())
            {
                _logger.LogInformation("No workflow stages found that use data source {DataSourceId} as input", asset.DataSourceId);
                return;
            }

            _logger.LogInformation("Found {StageCount} workflow stages that use data source {DataSourceId} as input", 
                relevantStages.Count, asset.DataSourceId);

            // For each stage, create tasks for the asset in that workflow/stage
            foreach (var stage in relevantStages)
            {
                try
                {
                    var tasksCreated = await _taskService.CreateTasksForDataSourceAsync(
                        asset.ProjectId, 
                        stage.WorkflowId, 
                        stage.WorkflowStageId, 
                        asset.DataSourceId);
                        
                    _logger.LogInformation("Created {TasksCreated} tasks for workflow stage {StageId} ({StageName}) for new asset {AssetId}", 
                        tasksCreated, stage.WorkflowStageId, stage.Name, asset.AssetId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create tasks for workflow stage {StageId} for new asset {AssetId}", 
                        stage.WorkflowStageId, asset.AssetId);
                    // Continue with other stages even if one fails
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tasks for newly imported asset {AssetId}", asset.AssetId);
            // Don't throw - asset import should succeed even if task creation fails
        }
    }

    public async Task<UploadResultDto> UploadAssetAsync(int projectId, UploadAssetDto uploadDto)
    {
        _logger.LogInformation("Starting upload for file '{FileName}' in project {ProjectId}", uploadDto.File.FileName, projectId);

        try
        {
            // Validate file
            var validationError = ValidateFile(uploadDto.File);
            if (validationError != null)
            {
                return CreateFailedUploadResult(uploadDto.File.FileName, validationError, ErrorType.ValidationError);
            }

            // Generate unique file name and storage path
            var sanitizedFileName = FileProcessingUtils.SanitizeFileName(uploadDto.File.FileName);
            var uniqueObjectName = FileProcessingUtils.GenerateUniqueFileName(sanitizedFileName);
            var mimeType = FileProcessingUtils.GetMimeType(uploadDto.File.FileName);

            // Extract image dimensions and validate if it's an image
            (int Width, int Height)? dimensions = null;
            using (var stream = uploadDto.File.OpenReadStream())
            {
                if (mimeType.StartsWith("image/"))
                {
                    // Use ImageSharp for robust image validation and dimension extraction
                    if (!FileProcessingUtils.IsValidImageStream(stream))
                    {
                        return CreateFailedUploadResult(uploadDto.File.FileName, "File appears to be corrupted or is not a valid image", ErrorType.ValidationError);
                    }
                    
                    dimensions = FileProcessingUtils.GetImageDimensions(stream);
                }
            }

            // Get data source to determine bucket name
            var dataSource = await _dataSourceRepository.GetByIdAsync(uploadDto.DataSourceId);
            if (dataSource == null)
            {
                return CreateFailedUploadResult(uploadDto.File.FileName, $"DataSource with ID {uploadDto.DataSourceId} not found", ErrorType.ValidationError);
            }

            string bucketName = _storageService.GenerateBucketName(projectId, dataSource.Name);

            // Upload file to storage using dynamic bucket naming
            string storagePath;
            using (var stream = uploadDto.File.OpenReadStream())
            {
                storagePath = await _fileStorageService.UploadFileAsync(stream, bucketName, uniqueObjectName);
            }

            // Create asset record in database
            var asset = new Asset
            {
                ExternalId = storagePath,
                Filename = sanitizedFileName,
                MimeType = mimeType,
                SizeBytes = uploadDto.File.Length,
                Width = dimensions?.Width,
                Height = dimensions?.Height,
                Metadata = uploadDto.Metadata,
                ProjectId = projectId,
                DataSourceId = uploadDto.DataSourceId,
                Status = AssetStatus.IMPORTED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _assetRepository.AddAsync(asset);
            await _assetRepository.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded asset with ID: {AssetId} for file '{FileName}'", asset.AssetId, uploadDto.File.FileName);

            return new UploadResultDto
            {
                Asset = MapToDto(asset),
                Filename = uploadDto.File.FileName,
                Success = true
            };
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorType.ValidationError);
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Storage error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, ex.Message, ErrorType.StorageError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading file '{FileName}': {Message}", uploadDto.File.FileName, ex.Message);
            return CreateFailedUploadResult(uploadDto.File.FileName, "An unexpected error occurred during upload", ErrorType.InternalServerError);
        }
    }

    public async Task<BulkUploadResultDto> UploadAssetsAsync(int projectId, BulkUploadAssetDto bulkUploadDto)
    {
        _logger.LogInformation("Starting bulk upload of {FileCount} files for project {ProjectId}", bulkUploadDto.Files.Count, projectId);

        // Validate that files are provided
        if (bulkUploadDto.Files == null || bulkUploadDto.Files.Count == 0)
        {
            _logger.LogWarning("Bulk upload attempted with no files for project {ProjectId}", projectId);
            return new BulkUploadResultDto
            {
                Results = [],
                TotalFiles = 0,
                SuccessfulUploads = 0,
                FailedUploads = 0,
                Summary = "No files provided for upload"
            };
        }

        var results = new List<UploadResultDto>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var file in bulkUploadDto.Files)
        {
            var uploadDto = new UploadAssetDto
            {
                File = file,
                DataSourceId = bulkUploadDto.DataSourceId,
                Metadata = bulkUploadDto.Metadata
            };

            var result = await UploadAssetAsync(projectId, uploadDto);
            results.Add(result);

            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        var summary = $"Uploaded {successCount} of {bulkUploadDto.Files.Count} files successfully";
        if (failureCount > 0)
        {
            summary += $", {failureCount} failed";
        }

        _logger.LogInformation("Bulk upload completed for project {ProjectId}: {Summary}", projectId, summary);

        return new BulkUploadResultDto
        {
            Results = results,
            TotalFiles = bulkUploadDto.Files.Count,
            SuccessfulUploads = successCount,
            FailedUploads = failureCount,
            Summary = summary
        };
    }

    public async Task<bool> ValidateAssetBelongsToProjectAsync(int assetId, int projectId)
    {
        _logger.LogInformation("Validating that asset {AssetId} belongs to project {ProjectId}", assetId, projectId);

        var asset = await _assetRepository.GetByIdAsync(assetId);
        if (asset == null)
        {
            _logger.LogWarning("Asset with ID: {AssetId} not found.", assetId);
            return false;
        }

        return asset.ProjectId == projectId;
    }

    #region Private Helper Methods
    private static string? ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "File is empty";
        }

        if (file.Length > FileProcessingUtils.MaxFileSizeBytes)
        {
            return $"File size exceeds maximum allowed size of {FileProcessingUtils.MaxFileSizeBytes / (1024 * 1024)}MB";
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            return "File name is required";
        }

        if (!FileProcessingUtils.IsValidImageFile(file.FileName, file.ContentType))
        {
            return "File type is not supported. Only image files are allowed.";
        }

        // Additional validation using ImageSharp to ensure the file is actually a valid image
        try
        {
            using var stream = file.OpenReadStream();
            if (!FileProcessingUtils.IsValidImageStream(stream))
            {
                return "File is not a valid image or is corrupted.";
            }
        }
        catch (Exception)
        {
            return "Unable to process the image file. It may be corrupted.";
        }

        return null;
    }

    private static UploadResultDto CreateFailedUploadResult(string filename, string errorMessage, ErrorType errorType)
    {
        return new UploadResultDto
        {
            Filename = filename,
            Success = false,
            ErrorMessage = errorMessage,
            ErrorType = errorType
        };
    }

    private static AssetDto MapToDto(Asset asset, string? imageUrl = null)
    {
        return new AssetDto
        {
            Id = asset.AssetId,
            Filename = asset.Filename,
            MimeType = asset.MimeType,
            SizeBytes = asset.SizeBytes,
            Width = asset.Width,
            Height = asset.Height,
            DurationMs = asset.DurationMs,
            Status = asset.Status,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt,
            ImageUrl = imageUrl
        };
    }

    public async Task<AssetMovementResult> HandleTaskWorkflowAssetMovementAsync(LaberisTask task, TaskStatus targetStatus, string userId)
    {
        _logger.LogInformation("Handling asset movement for task {TaskId} transitioning to {TargetStatus}", 
            task.TaskId, targetStatus);

        var result = new AssetMovementResult();

        try
        {
            // Handle completion in annotation/review stages (move to next stage)
            if (targetStatus == TaskStatus.COMPLETED)
            {
                await HandleTaskCompletionMovement(task, result, userId);
            }
            // Handle manager marking task incomplete in completion stage (move back to annotation)
            else if (targetStatus == TaskStatus.READY_FOR_ANNOTATION && 
                     task.CurrentWorkflowStage?.StageType == WorkflowStageType.COMPLETION)
            {
                await HandleTaskIncompleteMovement(task, result, userId);
            }

            _logger.LogInformation("Asset movement completed for task {TaskId}: {AssetMoved}, {NewTasksCreated} new tasks", 
                task.TaskId, result.AssetMoved, result.NewTasksCreated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling asset movement for task {TaskId}", task.TaskId);
            result.ErrorMessage = $"Asset movement failed: {ex.Message}";
        }

        return result;
    }

    private async System.Threading.Tasks.Task HandleTaskCompletionMovement(LaberisTask task, AssetMovementResult result, string userId)
    {
        var currentStageType = task.CurrentWorkflowStage?.StageType;
        
        // Only move assets for annotation and review stages
        if (currentStageType != WorkflowStageType.ANNOTATION && currentStageType != WorkflowStageType.REVISION)
        {
            return;
        }

        // Archive the current task (as per our design)
        result.ShouldArchiveTask = true;
        
        // Find and create tasks for next workflow stage
        var nextStage = await _taskRepository.GetNextWorkflowStageAsync(task.CurrentWorkflowStageId);
        
        if (nextStage != null && nextStage.InputDataSourceId.HasValue)
        {
            try
            {
                // TODO: Implement actual asset movement between MinIO data sources
                // For now, we just create tasks - asset movement would happen here
                
                var tasksCreated = await _taskService.CreateTasksForDataSourceAsync(
                    task.ProjectId, 
                    task.WorkflowId, 
                    nextStage.WorkflowStageId, 
                    nextStage.InputDataSourceId.Value);

                result.AssetMoved = true; // Would be true after actual MinIO operation
                result.NewTasksCreated = tasksCreated;
                result.TargetWorkflowStageId = nextStage.WorkflowStageId;

                _logger.LogInformation("Created {TasksCreated} tasks for next stage {NextStageId} after completing task {TaskId}", 
                    tasksCreated, nextStage.WorkflowStageId, task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tasks for next stage {NextStageId} after completing task {TaskId}", 
                    nextStage.WorkflowStageId, task.TaskId);
                // Don't fail the status change if we can't create next stage tasks
                result.ErrorMessage = $"Failed to create tasks for next stage: {ex.Message}";
            }
        }
    }

    private async System.Threading.Tasks.Task HandleTaskIncompleteMovement(LaberisTask task, AssetMovementResult result, string userId)
    {
        // Special case: Manager marking task incomplete in completion stage
        // This should create a NEW task in annotation stage (as per our design)
        var annotationStage = await _taskRepository.GetInitialWorkflowStageAsync(task.WorkflowId);
        
        if (annotationStage != null && annotationStage.InputDataSourceId.HasValue)
        {
            try
            {
                // TODO: Implement actual asset movement back to annotation data source
                // For now, we just create the task
                
                var tasksCreated = await _taskService.CreateTasksForDataSourceAsync(
                    task.ProjectId, 
                    task.WorkflowId, 
                    annotationStage.WorkflowStageId, 
                    annotationStage.InputDataSourceId.Value);

                result.AssetMoved = true; // Would be true after actual MinIO operation
                result.NewTasksCreated = tasksCreated;
                result.TargetWorkflowStageId = annotationStage.WorkflowStageId;
                
                _logger.LogInformation("Created new annotation task for asset {AssetId} after marking task {TaskId} incomplete", 
                    task.AssetId, task.TaskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new annotation task for asset {AssetId} after marking task {TaskId} incomplete", 
                    task.AssetId, task.TaskId);
                result.ErrorMessage = $"Failed to create new annotation task: {ex.Message}";
            }
        }
    }

    #endregion
}
