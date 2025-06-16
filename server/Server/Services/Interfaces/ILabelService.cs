using server.Models.Common;
using server.Models.DTOs.Label;

namespace server.Services.Interfaces
{
    public interface ILabelService
    {
        /// <summary>
        /// Retrieves all labels for a specific label scheme, optionally filtered and sorted.
        /// </summary>
        /// <param name="schemeId">The ID of the label scheme to retrieve labels for.</param>
        /// <param name="filterOn">The field to filter on.</param>
        /// <param name="filterQuery">The query string to filter by.</param>
        /// <param name="sortBy">The field to sort by.</param>
        /// <param name="isAscending">True for ascending order, false for descending.</param>
        /// <param name="pageNumber">The page number for pagination (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of LabelDto.</returns>
        Task<PaginatedResponse<LabelDto>> GetLabelsForSchemeAsync(
            int schemeId,
            string? filterOn = null, string? filterQuery = null, string? sortBy = null,
            bool isAscending = true, int pageNumber = 1, int pageSize = 25
        );

        /// <summary>
        /// Retrieves a label by its ID.
        /// </summary>
        /// <param name="labelId">The ID of the label to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation, containing the LabelDto if found, otherwise null.</returns>
        Task<LabelDto?> GetLabelByIdAsync(int labelId);

        /// <summary>
        /// Creates a new label under a specific label scheme.
        /// </summary>
        /// <param name="schemeId">The ID of the label scheme to create the label under.</param>
        /// <param name="createDto">The DTO containing information for the new label.</param>
        /// <returns>A task that represents the asynchronous operation, containing the created LabelDto if successful, otherwise null.</returns>
        Task<LabelDto?> CreateLabelAsync(int schemeId, CreateLabelDto createDto);

        /// <summary>
        /// Updates an existing label.
        /// </summary>
        /// <param name="labelId">The ID of the label to update.</param>
        /// <param name="updateDto">The DTO containing updated label information.</param>
        /// <returns>A task that represents the asynchronous operation, containing the updated LabelDto if successful, otherwise null.</returns>
        Task<LabelDto?> UpdateLabelAsync(int labelId, UpdateLabelDto updateDto);

        /// <summary>
        /// Deletes a label by its ID.
        /// </summary>
        /// <param name="labelId">The ID of the label to delete.</param>
        /// <returns>A task that represents the asynchronous operation, returning true if the label was successfully deleted, otherwise false.</returns>
        Task<bool> DeleteLabelAsync(int labelId);
    }
}