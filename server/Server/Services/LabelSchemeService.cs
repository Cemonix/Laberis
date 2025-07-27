using Microsoft.EntityFrameworkCore;
using server.Models.Domain;
using server.Models.Common;
using server.Models.DTOs.LabelScheme;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class LabelSchemeService : ILabelSchemeService
{
    private readonly ILabelSchemeRepository _labelSchemeRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILabelRepository _labelRepository;
    private readonly IAnnotationRepository _annotationRepository;
    private readonly ILogger<LabelSchemeService> _logger;

    public LabelSchemeService(
        ILabelSchemeRepository labelSchemeRepository, 
        IProjectRepository projectRepository,
        ILabelRepository labelRepository,
        IAnnotationRepository annotationRepository,
        ILogger<LabelSchemeService> logger)
    {
        _labelSchemeRepository = labelSchemeRepository;
        _projectRepository = projectRepository;
        _labelRepository = labelRepository;
        _annotationRepository = annotationRepository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<LabelSchemeDto>> GetLabelSchemesForProjectAsync(
        int projectId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        var (schemes, totalCount) = await _labelSchemeRepository.GetAllWithCountAsync(
            filter: ls => ls.ProjectId == projectId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy,
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        var schemeDtos = schemes.Select(ls => new LabelSchemeDto
        {
            Id = ls.LabelSchemeId,
            Name = ls.Name,
            Description = ls.Description,
            IsDefault = ls.IsDefault,
            IsActive = ls.IsActive,
            ProjectId = ls.ProjectId,
            CreatedAt = ls.CreatedAt,
            DeletedAt = ls.DeletedAt
        }).ToArray();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<LabelSchemeDto>
        {
            Data = schemeDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
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
            IsActive = scheme.IsActive,
            ProjectId = scheme.ProjectId,
            CreatedAt = scheme.CreatedAt,
            DeletedAt = scheme.DeletedAt
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
        var existingScheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId && ls.IsActive)).FirstOrDefault();
        if (existingScheme == null) return null;

        // Update properties directly on the tracked entity
        existingScheme.Name = updateDto.Name ?? existingScheme.Name;
        existingScheme.Description = updateDto.Description;
        existingScheme.UpdatedAt = DateTime.UtcNow;

        try
        {
            // No need to call Update() - EF will detect changes automatically
            await _labelSchemeRepository.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update label scheme {SchemeId} due to a database constraint.", schemeId);
            return null; // TODO: Rethrow or handle this more gracefully in the future
        }

        return await GetLabelSchemeByIdAsync(projectId, existingScheme.LabelSchemeId);
    }

    public async Task<bool> DeleteLabelSchemeAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId && ls.IsActive)).FirstOrDefault();
        if (scheme == null) return false;

        // TODO: This is hard delete, which will remove all associated labels. Consider if this is the desired behavior.
        _labelSchemeRepository.Remove(scheme);
        await _labelSchemeRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteLabelSchemeAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId && ls.IsActive)).FirstOrDefault();
        if (scheme == null) return false;

        var now = DateTime.UtcNow;
        
        // Soft delete the scheme - update properties directly
        scheme.IsActive = false;
        scheme.DeletedAt = now;
        scheme.UpdatedAt = now;

        // Soft delete all labels in the scheme
        var labels = await _labelRepository.FindAsync(l => l.LabelSchemeId == schemeId && l.IsActive);
        foreach (var label in labels)
        {
            label.IsActive = false;
            label.DeletedAt = now;
            label.UpdatedAt = now;
        }

        await _labelSchemeRepository.SaveChangesAsync();
        return true;
    }

    public async Task<LabelSchemeDeletionImpactDto?> GetDeletionImpactAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId && ls.IsActive)).FirstOrDefault();
        if (scheme == null) return null;

        var labels = await _labelRepository.FindAsync(l => l.LabelSchemeId == schemeId && l.IsActive);
        var labelImpacts = new List<LabelImpactDto>();

        foreach (var label in labels)
        {
            var annotationsCount = await _annotationRepository.CountAsync(a => a.LabelId == label.LabelId);
            labelImpacts.Add(new LabelImpactDto
            {
                LabelId = label.LabelId,
                LabelName = label.Name,
                LabelColor = label.Color,
                AnnotationsCount = annotationsCount
            });
        }

        return new LabelSchemeDeletionImpactDto
        {
            LabelSchemeId = scheme.LabelSchemeId,
            LabelSchemeName = scheme.Name,
            TotalLabelsCount = labels.Count(),
            TotalAnnotationsCount = labelImpacts.Sum(li => li.AnnotationsCount),
            LabelImpacts = labelImpacts
        };
    }

    public async Task<bool> ReactivateLabelSchemeAsync(int projectId, int schemeId)
    {
        var scheme = (await _labelSchemeRepository.FindAsync(ls => ls.LabelSchemeId == schemeId && ls.ProjectId == projectId && !ls.IsActive)).FirstOrDefault();
        if (scheme == null) return false;

        var now = DateTime.UtcNow;
        
        // Check for name conflicts with active schemes
        var existingActiveScheme = (await _labelSchemeRepository.FindAsync(ls => ls.ProjectId == projectId && ls.Name == scheme.Name && ls.IsActive)).FirstOrDefault();
        if (existingActiveScheme != null)
        {
            _logger.LogWarning("Cannot reactivate label scheme {SchemeId} because another active scheme with name '{Name}' already exists", schemeId, scheme.Name);
            return false;
        }

        // Reactivate the scheme - update properties directly
        scheme.IsActive = true;
        scheme.DeletedAt = null;
        scheme.UpdatedAt = now;

        // Reactivate all labels in the scheme (check for conflicts too)
        var labels = await _labelRepository.FindAsync(l => l.LabelSchemeId == schemeId && !l.IsActive);
        foreach (var label in labels)
        {
            label.IsActive = true;
            label.DeletedAt = null;
            label.UpdatedAt = now;
        }

        await _labelSchemeRepository.SaveChangesAsync();
        return true;
    }
}