using System;
using server.Models.DTOs.LabelScheme;

namespace server.Services.Interfaces;

public interface ILabelSchemeService
{
    /// <summary>
    /// Retrieves all label schemes for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <returns>A collection of label schemes associated with the project.</returns>
    Task<IEnumerable<LabelSchemeDto>> GetLabelSchemesForProjectAsync(int projectId);

    /// <summary>
    /// Retrieves a specific label scheme by its ID within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme.</param>
    /// <returns>The label scheme if found, otherwise null.</returns>
    Task<LabelSchemeDto?> GetLabelSchemeByIdAsync(int projectId, int schemeId);

    /// <summary>
    /// Creates a new label scheme for a specific project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="createDto">The DTO containing the details of the label scheme to create.</param>
    /// <returns>The created label scheme if successful, otherwise null.</returns>
    Task<LabelSchemeDto?> CreateLabelSchemeAsync(int projectId, CreateLabelSchemeDto createDto);

    /// <summary>
    /// Updates an existing label scheme within a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme to update.</param>
    /// <param name="updateDto">The DTO containing the updated details of the label scheme.</param>
    /// <returns>The updated label scheme if successful, otherwise null.</returns>
    Task<LabelSchemeDto?> UpdateLabelSchemeAsync(int projectId, int schemeId, UpdateLabelSchemeDto updateDto);

    /// <summary>
    /// Deletes a label scheme from a project.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="schemeId">The ID of the label scheme to delete.</param>
    /// <returns>True if the label scheme was successfully deleted, otherwise false.</returns>
    Task<bool> DeleteLabelSchemeAsync(int projectId, int schemeId);
}
