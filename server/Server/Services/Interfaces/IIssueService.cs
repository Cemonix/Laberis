using server.Models.DTOs.Issue;

namespace server.Services.Interfaces;

public interface IIssueService
{
    /// <summary>
    /// Retrieves all issues for a specific asset, optionally filtered and sorted.
    /// </summary>
    /// <param name="assetId">The ID of the asset to retrieve issues for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "status", "priority", "issue_type", "assigned_to_user_id").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "priority", "status").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of IssueDto.</returns>
    Task<IEnumerable<IssueDto>> GetIssuesForAssetAsync(
        int assetId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves all issues assigned to a specific user, optionally filtered and sorted.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve assigned issues for.</param>
    /// <param name="filterOn">The field to filter on (e.g., "status", "priority", "issue_type").</param>
    /// <param name="filterQuery">The query string to filter by.</param>
    /// <param name="sortBy">The field to sort by (e.g., "created_at", "priority", "status").</param>
    /// <param name="isAscending">True for ascending order, false for descending.</param>
    /// <param name="pageNumber">The page number for pagination (1-based index).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation, containing a collection of IssueDto.</returns>
    Task<IEnumerable<IssueDto>> GetIssuesForUserAsync(
        string userId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );

    /// <summary>
    /// Retrieves an issue by its ID.
    /// </summary>
    /// <param name="issueId">The ID of the issue to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation, containing the IssueDto if found, otherwise null.</returns>
    Task<IssueDto?> GetIssueByIdAsync(int issueId);

    /// <summary>
    /// Creates a new issue.
    /// </summary>
    /// <param name="createDto">The DTO containing information for the new issue.</param>
    /// <param name="reportedByUserId">The ID of the user reporting the issue.</param>
    /// <returns>A task that represents the asynchronous operation, containing the newly created IssueDto.</returns>
    Task<IssueDto> CreateIssueAsync(CreateIssueDto createDto, string reportedByUserId);

    /// <summary>
    /// Updates an existing issue.
    /// </summary>
    /// <param name="issueId">The ID of the issue to update.</param>
    /// <param name="updateDto">The DTO containing updated issue information.</param>
    /// <returns>A task that represents the asynchronous operation, containing the updated IssueDto if successful, otherwise null.</returns>
    Task<IssueDto?> UpdateIssueAsync(int issueId, UpdateIssueDto updateDto);

    /// <summary>
    /// Deletes an issue by its ID.
    /// </summary>
    /// <param name="issueId">The ID of the issue to delete.</param>
    /// <returns>A task that represents the asynchronous operation, returning true if the issue was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteIssueAsync(int issueId);
}
