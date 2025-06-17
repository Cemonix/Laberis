using Moq;
using Microsoft.Extensions.Logging;
using Server.Tests.Factories;
using server.Services;
using server.Repositories.Interfaces;
using server.Models.Domain;
using server.Models.DTOs.Asset;
using server.Models.Domain.Enums;
using System.Linq.Expressions;
using server.Services.Interfaces;

namespace Server.Tests.Services
{
    public class AssetServiceTests : IDisposable
    {
        private readonly DbContextFactory _dbContextFactory;
        private readonly Mock<IAssetRepository> _mockAssetRepository;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly Mock<IDataSourceRepository> _mockDataSourceRepository;
        private readonly Mock<ILogger<AssetService>> _mockLogger;
        private readonly AssetService _assetService;

        public AssetServiceTests()
        {
            _dbContextFactory = new DbContextFactory();
            _mockAssetRepository = new Mock<IAssetRepository>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            _mockDataSourceRepository = new Mock<IDataSourceRepository>();
            _mockLogger = new Mock<ILogger<AssetService>>();

            _assetService = new AssetService(
                _mockAssetRepository.Object,
                _mockFileStorageService.Object,
                _mockDataSourceRepository.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAssetByIdAsync_Should_ReturnAssetDto_WhenAssetExists()
        {
            // Arrange
            var assetId = 1;
            var asset = new Asset
            {
                AssetId = assetId,
                ExternalId = "test-external-id",
                Filename = "test.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 1024,
                Status = AssetStatus.IMPORTED,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockAssetRepository.Setup(r => r.GetByIdAsync(assetId))
                .ReturnsAsync(asset);

            // Act
            var result = await _assetService.GetAssetByIdAsync(assetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(assetId, result.Id);
            Assert.Equal("test.jpg", result.Filename);
            Assert.Equal(AssetStatus.IMPORTED, result.Status);
            
            _mockAssetRepository.Verify(r => r.GetByIdAsync(assetId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAssetByIdAsync_Should_ReturnNull_WhenAssetDoesNotExist()
        {
            // Arrange
            var assetId = 999;
            _mockAssetRepository.Setup(r => r.GetByIdAsync(assetId))
                .ReturnsAsync((Asset?)null);

            // Act
            var result = await _assetService.GetAssetByIdAsync(assetId);

            // Assert
            Assert.Null(result);
            _mockAssetRepository.Verify(r => r.GetByIdAsync(assetId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateAssetAsync_Should_ReturnAssetDto_WhenValidData()
        {
            // Arrange
            var projectId = 1;
            var createDto = new CreateAssetDto
            {
                ExternalId = "test-external-id",
                Filename = "new-asset.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 1024
            };

            _mockAssetRepository.Setup(r => r.AddAsync(It.IsAny<Asset>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            _mockAssetRepository.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _assetService.CreateAssetAsync(projectId, createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.ExternalId, result.ExternalId);
            Assert.Equal(createDto.Filename, result.Filename);
            Assert.Equal(createDto.MimeType, result.MimeType);
            Assert.Equal(projectId, result.ProjectId);
            Assert.Equal(AssetStatus.PENDING_IMPORT, result.Status);
            
            _mockAssetRepository.Verify(r => r.AddAsync(It.Is<Asset>(a => 
                a.ExternalId == createDto.ExternalId &&
                a.Filename == createDto.Filename &&
                a.ProjectId == projectId)), Times.Once
            );
            _mockAssetRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        public void Dispose()
        {
            _dbContextFactory.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
