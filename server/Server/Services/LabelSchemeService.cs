using Microsoft.EntityFrameworkCore;
using server.Models.Domain;
using server.Models.DTOs.LabelScheme;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class LabelSchemeService : ILabelSchemeService
{
    private readonly ILabelSchemeRepository _labelSchemeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<LabelSchemeService> _logger;

    public LabelSchemeService(ILabelSchemeRepository labelSchemeRepository, IProjectRepository projectRepository, ILogger<LabelSchemeService> logger)
    {
        _labelSchemeRepository = labelSchemeRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<LabelSchemeDto>> GetLabelSchemesForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        var schemes = await _labelSchemeRepository.GetAllAsync(
            filter: ls => ls.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        return schemes.Select(ls => new LabelSchemeDto
        {
            Id = ls.LabelSchemeId,
            Name = ls.Name,
            Description = ls.Description,
            IsDefault = ls.IsDefault,
            ProjectId = ls.ProjectId,
            CreatedAt = ls.CreatedAt
        });
    }

    public async Task<LabelSchemeDto?> GetLabelSchemeByIdAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId)).FirstOrDefault();
        if (scheme == null) return null;

        return new LabelSchemeDto
        {
            Id = scheme.LabelSchemeId,
            Name = scheme.Name,
            Description = scheme.Description,
            IsDefault = scheme.IsDefault,
            ProjectId = scheme.ProjectId,
            CreatedAt = scheme.CreatedAt
        };
    }

    public async Task<LabelSchemeDto?> CreateLabelSchemeAsync(int projectId, CreateLabelSchemeDto createDto)
    {
        var projectExists = await _projectRepository.GetByIdAsync(projectId) != null;
        if (!projectExists)
        {
            _logger.LogWarning("Attempted to create a label scheme for a non-existent project with ID: {ProjectId}", projectId);
            return null; // TODO: Consider throwing an exception or handling this case more gracefully
        }

        var newScheme = new LabelScheme
        {
            Name = createDto.Name,
            Description = createDto.Description,
            IsDefault = createDto.IsDefault,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _labelSchemeRepository.AddAsync(newScheme);
            await _labelSchemeRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // This will catch the unique constraint violation for (ProjectId, Name)
            _logger.LogError(
                ex,
                "Failed to create label scheme due to a database constraint. A scheme with name '{Name}' may already exist for project {ProjectId}",
                newScheme.Name,
                projectId
            );
            return null; // TODO: Rethrow or handle this more gracefully in the future
        }

        return await GetLabelSchemeByIdAsync(projectId, newScheme.LabelSchemeId);
    }

    public async Task<LabelSchemeDto?> UpdateLabelSchemeAsync(int projectId, int schemeId, UpdateLabelSchemeDto updateDto)
    {
        var existingScheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId)).FirstOrDefault();
        if (existingScheme == null) return null;

        var updatedScheme = existingScheme with
        {
            Name = updateDto.Name,
            Description = updateDto.Description,
            IsDefault = updateDto.IsDefault,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _labelSchemeRepository.Update(updatedScheme);
            await _labelSchemeRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update label scheme {SchemeId} due to a database constraint.", schemeId);
            return null; // TODO: Rethrow or handle this more gracefully in the future
        }

        return await GetLabelSchemeByIdAsync(projectId, updatedScheme.LabelSchemeId);
    }

    public async Task<bool> DeleteLabelSchemeAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId)).FirstOrDefault();
        if (scheme == null) return false;

        // TODO: This is hard delete, which will remove all associated labels. Consider if this is the desired behavior.
        _labelSchemeRepository.Remove(scheme);
        await _labelSchemeRepository.SaveChangesAsync();
        return true;
    }
}