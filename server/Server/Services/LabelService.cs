using server.Models.Domain;
using server.Models.Common;
using server.Models.DTOs.Label;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services
{
    public class LabelService : ILabelService
    {
        private readonly ILabelRepository _labelRepository;

        public LabelService(ILabelRepository labelRepository)
        {
            _labelRepository = labelRepository;
        }

        public async Task<PaginatedResponse<LabelDto>> GetLabelsForSchemeAsync(
            int schemeId,
            string? filterOn = null, string? filterQuery = null, string? sortBy = null,
            bool isAscending = true, int pageNumber = 1, int pageSize = 25)
        {
            var (labels, totalCount) = await _labelRepository.GetAllWithCountAsync(
                filter: l => l.LabelSchemeId == schemeId,
                filterOn: filterOn,
                filterQuery: filterQuery,
                sortBy: sortBy,
                isAscending: isAscending,
                pageNumber: pageNumber,
                pageSize: pageSize
            );

            var labelDtos = labels.Select(l => new LabelDto
            {
                Id = l.LabelId,
                Name = l.Name,
                Description = l.Description,
                Color = l.Color,
                LabelSchemeId = l.LabelSchemeId,
                CreatedAt = l.CreatedAt
            }).ToArray();

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PaginatedResponse<LabelDto>
            {
                Data = labelDtos,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalCount
            };
        }

        public async Task<LabelDto?> GetLabelByIdAsync(int labelId)
        {
            var label = await _labelRepository.GetByIdAsync(labelId);
            if (label == null) return null;

            return new LabelDto
            {
                Id = label.LabelId,
                Name = label.Name,
                Description = label.Description,
                Color = label.Color,
                LabelSchemeId = label.LabelSchemeId,
                CreatedAt = label.CreatedAt
            };
        }

        public async Task<LabelDto?> CreateLabelAsync(int schemeId, CreateLabelDto createDto)
        {
            var newLabel = new Label
            {
                Name = createDto.Name,
                Description = createDto.Description,
                Color = createDto.Color,
                LabelSchemeId = schemeId
            };

            await _labelRepository.AddAsync(newLabel);
            await _labelRepository.SaveChangesAsync();

            return await GetLabelByIdAsync(newLabel.LabelId);
        }

        public async Task<LabelDto?> UpdateLabelAsync(int labelId, UpdateLabelDto updateDto)
        {
            var existingLabel = await _labelRepository.GetByIdAsync(labelId);
            if (existingLabel == null) return null;
            
            // Update properties directly on the tracked entity
            existingLabel.Name = updateDto.Name ?? existingLabel.Name;
            existingLabel.Description = updateDto.Description;
            existingLabel.Color = updateDto.Color ?? existingLabel.Color;
            
            await _labelRepository.SaveChangesAsync();

            return await GetLabelByIdAsync(labelId);
        }

        public async Task<bool> DeleteLabelAsync(int labelId)
        {
            var label = await _labelRepository.GetByIdAsync(labelId);
            if (label == null) return false;

            _labelRepository.Remove(label);
            await _labelRepository.SaveChangesAsync();
            return true;
        }
    }
}