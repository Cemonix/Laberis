using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Models.DTOs.DataSource;
using server.Services.Interfaces;

namespace server.Controllers
{
    [Route("api/projects/{projectId:int}/datasources")]
    [ApiController]
    [Authorize]
    public class DataSourcesController : ControllerBase
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly ILogger<DataSourcesController> _logger;

        public DataSourcesController(IDataSourceService dataSourceService, ILogger<DataSourcesController> logger)
        {
            _dataSourceService = dataSourceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDataSourcesForProject(int projectId)
        {
            var dataSources = await _dataSourceService.GetAllDataSourcesForProjectAsync(projectId);
            return Ok(dataSources);
        }

        [HttpGet("{dataSourceId:int}")]
        public async Task<IActionResult> GetDataSourceById(int projectId, int dataSourceId)
        {
            var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
            if (dataSource == null || dataSource.ProjectId != projectId)
            {
                return NotFound();
            }
            return Ok(dataSource);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDataSource(int projectId, [FromBody] CreateDataSourceDto createDto)
        {
            var newDataSource = await _dataSourceService.CreateDataSourceAsync(projectId, createDto);
            if (newDataSource == null)
            {
                return BadRequest("Failed to create the data source. Ensure the project exists.");
            }
            return CreatedAtAction(nameof(GetDataSourceById), new { projectId, dataSourceId = newDataSource.Id }, newDataSource);
        }

        [HttpPut("{dataSourceId:int}")]
        public async Task<IActionResult> UpdateDataSource(int projectId, int dataSourceId, [FromBody] UpdateDataSourceDto updateDto)
        {
            var updatedDataSource = await _dataSourceService.UpdateDataSourceAsync(dataSourceId, updateDto);
            if (updatedDataSource == null || updatedDataSource.ProjectId != projectId)
            {
                return NotFound();
            }
            return Ok(updatedDataSource);
        }

        [HttpDelete("{dataSourceId:int}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<IActionResult> DeleteDataSource(int projectId, int dataSourceId)
        {
            var dataSource = await _dataSourceService.GetDataSourceByIdAsync(dataSourceId);
            if (dataSource == null || dataSource.ProjectId != projectId)
            {
                return NotFound();
            }

            var success = await _dataSourceService.DeleteDataSourceAsync(dataSourceId);
            if (!success)
            {
                // This case should ideally not be hit if the above check passes, but is good for safety.
                return NotFound();
            }
            return NoContent();
        }
    }
}