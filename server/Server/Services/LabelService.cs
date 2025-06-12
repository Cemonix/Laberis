using server.Models.Domain;
using server.Models.DTOs.Label;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services
{
    public class LabelService : ILabelService
    {
        private readonly ILabelRepository _labelRepository;
        private readonly ILogger<LabelService> _logger;

        public LabelService(ILabelRepository labelRepository, ILogger<LabelService> logger)
        {
            _labelRepository = labelRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<LabelDto>> GetLabelsForSchemeAsync(int schemeId)
        {
            var labels = await _labelRepository.FindAsync(l => l.LabelSchemeId == schemeId);
            return labels.Select(l => new LabelDto
            {
                Id = l.LabelId,
                Name = l.Name,
                Description = l.Description,
                Color = l.Color,
                LabelSchemeId = l.LabelSchemeId,
                CreatedAt = l.CreatedAt
            });
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
            
            var updatedLabel = existingLabel with
            {
                Name = updateDto.Name,
                Description = updateDto.Description,
                Color = updateDto.Color
            };

            _labelRepository.Update(updatedLabel);
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