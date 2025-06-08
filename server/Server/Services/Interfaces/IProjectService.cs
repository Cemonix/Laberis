using System;
using server.Models.DTOs;

namespace server.Services.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectDto>> GetAllProjectsAsync(
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25
    );
}
