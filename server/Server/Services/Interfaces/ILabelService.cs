using server.Models.DTOs.Label;

namespace server.Services.Interfaces
{
    public interface ILabelService
    {
        /// <summary>
        /// Retrieves all labels for a specific label scheme, optionally filtered and sorted.
        /// </summary>
        /// <param name="schemeId">The ID of the label scheme to retrieve labels for.</param>
        /// <returns>A task that represents the asynchronous operation, containing a collection of LabelDto.</returns>
        Task<IEnumerable<LabelDto>> GetLabelsForSchemeAsync(int schemeId);

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