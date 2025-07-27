using server.Models.Domain;
using server.Models.DTOs.Issue;
using server.Models.Domain.Enums;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class IssueService : IIssueService
{
    private readonly IIssueRepository _issueRepository;
    private readonly ILogger<IssueService> _logger;

    public IssueService(
        IIssueRepository issueRepository,
        ILogger<IssueService> logger)
    {
        _issueRepository = issueRepository ?? throw new ArgumentNullException(nameof(issueRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<IssueDto>> GetIssuesForAssetAsync(
        int assetId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching issues for asset: {AssetId}", assetId);

        var (issues, totalCount) = await _issueRepository.GetAllWithCountAsync(
            filter: i => i.AssetId == assetId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} issues for asset: {AssetId}", issues.Count(), assetId);

        var issueDtos = issues.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<IssueDto>
        {
            Data = issueDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<PaginatedResponse<IssueDto>> GetIssuesForUserAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching issues for user: {UserId}", userId);

        var (issues, totalCount) = await _issueRepository.GetAllWithCountAsync(
            filter: i => i.AssignedToUserId == userId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} issues for user: {UserId}", issues.Count(), userId);

        var issueDtos = issues.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<IssueDto>
        {
            Data = issueDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<IssueDto?> GetIssueByIdAsync(int issueId)
    {
        _logger.LogInformation("Fetching issue with ID: {IssueId}", issueId);

        var issue = await _issueRepository.GetByIdAsync(issueId);

        if (issue == null)
        {
            _logger.LogWarning("Issue with ID: {IssueId} not found.", issueId);
            return null;
        }

        _logger.LogInformation("Successfully fetched issue with ID: {IssueId}", issueId);
        return MapToDto(issue);
    }

    public async Task<IssueDto> CreateIssueAsync(CreateIssueDto createDto, string reportedByUserId)
    {
        _logger.LogInformation("Creating new issue for asset: {AssetId}", createDto.AssetId);

        var issue = new Issue
        {
            Description = createDto.Description,
            Status = IssueStatus.OPEN, // Default status
            Priority = createDto.Priority,
            IssueType = createDto.IssueType,
            AssetId = createDto.AssetId,
            TaskId = createDto.TaskId,
            AnnotationId = createDto.AnnotationId,
            AssignedToUserId = createDto.AssignedToUserId,
            ReportedByUserId = reportedByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _issueRepository.AddAsync(issue);
        await _issueRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created issue with ID: {IssueId}", issue.IssueId);

        return MapToDto(issue);
    }

    public async Task<IssueDto?> UpdateIssueAsync(int issueId, UpdateIssueDto updateDto)
    {
        _logger.LogInformation("Updating issue with ID: {IssueId}", issueId);

        var existingIssue = await _issueRepository.GetByIdAsync(issueId);
        if (existingIssue == null)
        {
            _logger.LogWarning("Issue with ID: {IssueId} not found for update.", issueId);
            return null;
        }

        existingIssue.Description = updateDto.Description ?? existingIssue.Description;
        existingIssue.Status = updateDto.Status ?? existingIssue.Status;
        existingIssue.Priority = updateDto.Priority ?? existingIssue.Priority;
        existingIssue.IssueType = updateDto.IssueType ?? existingIssue.IssueType;
        existingIssue.ResolutionDetails = updateDto.ResolutionDetails ?? existingIssue.ResolutionDetails;
        existingIssue.AssignedToUserId = updateDto.AssignedToUserId ?? existingIssue.AssignedToUserId;
        existingIssue.UpdatedAt = DateTime.UtcNow;

        await _issueRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated issue with ID: {IssueId}", issueId);
        return MapToDto(existingIssue);
    }

    public async Task<bool> DeleteIssueAsync(int issueId)
    {
        _logger.LogInformation("Deleting issue with ID: {IssueId}", issueId);

        var issue = await _issueRepository.GetByIdAsync(issueId);

        if (issue == null)
        {
            _logger.LogWarning("Issue with ID: {IssueId} not found for deletion.", issueId);
            return false;
        }

        _issueRepository.Remove(issue);
        await _issueRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted issue with ID: {IssueId}", issueId);
        return true;
    }

    private static IssueDto MapToDto(Issue issue)
    {
        return new IssueDto
        {
            Id = issue.IssueId,
            Description = issue.Description,
            Status = issue.Status,
            Priority = issue.Priority,
            IssueType = issue.IssueType,
            ResolutionDetails = issue.ResolutionDetails,
            ResolvedAt = issue.ResolvedAt,
            CreatedAt = issue.CreatedAt,
            UpdatedAt = issue.UpdatedAt,
            TaskId = issue.TaskId,
            AssetId = issue.AssetId,
            AnnotationId = issue.AnnotationId,
            ReportedByEmail = issue.ReportedByUser?.Email ?? string.Empty,
            AssignedToEmail = issue.AssignedToUser?.Email
        };
    }
}
