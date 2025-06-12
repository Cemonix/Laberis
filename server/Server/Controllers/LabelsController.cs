using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Services.Interfaces;
using server.Models.DTOs.Label;

namespace server.Controllers
{
    [Route("api/projects/{projectId:int}/labelschemes/{schemeId:int}/[controller]")]
    [ApiController]
    [Authorize]
    public class LabelsController : ControllerBase
    {
        private readonly ILabelService _labelService;

        public LabelsController(ILabelService labelService)
        {
            _labelService = labelService ?? throw new ArgumentNullException(nameof(labelService));
        }

        /// <summary>
        /// Gets all labels for a specific label scheme.
        /// </summary>
        /// <param name="schemeId">The ID of the label scheme.</param>
        /// <param name="filterOn">The field to filter on.</param>
        /// <param name="filterQuery">The query string to filter by.</param>
        /// <param name="sortBy">The field to sort by.</param>
        /// <param name="isAscending">True for ascending order, false for descending.</param>
        /// <param name="pageNumber">The page number for pagination (1-based index).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A list of labels.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LabelDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLabelsForScheme(
            int schemeId,
            [FromQuery] string? filterOn = null, [FromQuery] string? filterQuery = null, [FromQuery] string? sortBy = null,
            [FromQuery] bool isAscending = true, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25)
        {
            var labels = await _labelService.GetLabelsForSchemeAsync(
                schemeId,
                filterOn,
                filterQuery,
                sortBy,
                isAscending,
                pageNumber,
                pageSize
            );
            return Ok(labels);
        }

        /// <summary>
        /// Gets a single label by its ID.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="schemeId">The ID of the label scheme.</param>
        /// <param name="labelId">The ID of the label.</param>
        /// <returns>The requested label.</returns>
        [HttpGet("{labelId:int}")]
        [ProducesResponseType(typeof(LabelDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLabelById(int projectId, int schemeId, int labelId)
        {
            var label = await _labelService.GetLabelByIdAsync(labelId);
            if (label == null || label.LabelSchemeId != schemeId)
            {
                return NotFound();
            }
            return Ok(label);
        }

        /// <summary>
        /// Creates a new label within a label scheme.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="schemeId">The ID of the label scheme.</param>
        /// <param name="createDto">The data for the new label.</param>
        /// <returns>The newly created label.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(LabelDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLabel(int projectId, int schemeId, [FromBody] CreateLabelDto createDto)
        {
            var newLabel = await _labelService.CreateLabelAsync(schemeId, createDto);
            if (newLabel == null)
            {
                return BadRequest("Failed to create the label.");
            }
            return CreatedAtAction(nameof(GetLabelById), new { projectId, schemeId, labelId = newLabel.Id }, newLabel);
        }

        /// <summary>
        /// Updates an existing label.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="schemeId">The ID of the label scheme.</param>
        /// <param name="labelId">The ID of the label to update.</param>
        /// <param name="updateDto">The updated data for the label.</param>
        /// <returns>The updated label.</returns>
        [HttpPut("{labelId:int}")]
        [ProducesResponseType(typeof(LabelDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLabel(int projectId, int schemeId, int labelId, [FromBody] UpdateLabelDto updateDto)
        {
            var updatedLabel = await _labelService.UpdateLabelAsync(labelId, updateDto);
            if (updatedLabel == null)
            {
                return NotFound();
            }
            return Ok(updatedLabel);
        }

        /// <summary>
        /// Deletes a label.
        /// </summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="schemeId">The ID of the label scheme.</param>
        /// <param name="labelId">The ID of the label to delete.</param>
        /// <returns>A confirmation of deletion.</returns>
        [HttpDelete("{labelId:int}")]
        [Authorize(Policy = "RequireAdminRole")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLabel(int projectId, int schemeId, int labelId)
        {
            var success = await _labelService.DeleteLabelAsync(labelId);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}