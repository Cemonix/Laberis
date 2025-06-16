using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using server.Controllers;
using server.Models.Common;
using server.Models.DTOs.Asset;
using server.Models.Domain.Enums;
using server.Services.Interfaces;

namespace Server.Tests.Controllers
{
    /// <summary>
    /// Unit tests for AssetsController. 
    /// Tests only the controller's HTTP behavior, error handling, and response formatting.
    /// Service layer is mocked to isolate controller logic.
    /// </summary>
    public class AssetsControllerTests
    {
        private readonly Mock<IAssetService> _mockAssetService;
        private readonly Mock<ILogger<AssetsController>> _mockLogger;
        private readonly AssetsController _controller;

        public AssetsControllerTests()
        {
            _mockAssetService = new Mock<IAssetService>();
            _mockLogger = new Mock<ILogger<AssetsController>>();
            _controller = new AssetsController(_mockAssetService.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenAssetServiceIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AssetsController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AssetsController(_mockAssetService.Object, null!));
        }

        #endregion

        #region GetAssetsForProject Tests

        [Fact]
        public async Task GetAssetsForProject_Should_ReturnOkResult_WithAssets()
        {
            // Arrange
            var projectId = 1;
            var assetDtos = new List<AssetDto>
            {
                new()
                {
                    Id = 1,
                    ExternalId = "asset-1",
                    Filename = "test1.jpg",
                    MimeType = "image/jpeg",
                    SizeBytes = 1024,
                    Status = AssetStatus.IMPORTED,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ProjectId = projectId
                },
                new() {
                    Id = 2,
                    ExternalId = "asset-2",
                    Filename = "test2.png",
                    MimeType = "image/png",
                    SizeBytes = 2048,
                    Status = AssetStatus.READY_FOR_ANNOTATION,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ProjectId = projectId
                }
            };

            var expectedAssets = new PaginatedResponse<AssetDto>
            {
                Data = assetDtos.ToArray(),
                PageSize = 25,
                CurrentPage = 1,
                TotalPages = 1,
                TotalItems = 2
            };

            _mockAssetService.Setup(s => s.GetAssetsForProjectAsync(
                projectId, null, null, null, true, 1, 25)
            ).ReturnsAsync(expectedAssets);

            // Act
            var result = await _controller.GetAssetsForProject(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var paginatedResponse = Assert.IsType<PaginatedResponse<AssetDto>>(okResult.Value);
            Assert.Equal(2, paginatedResponse.Data.Length);
            Assert.Equal(2, paginatedResponse.TotalItems);
            Assert.Equal(expectedAssets.Data, paginatedResponse.Data);

            _mockAssetService.Verify(s => s.GetAssetsForProjectAsync(
                projectId, null, null, null, true, 1, 25), Times.Once);
        }

        [Fact]
        public async Task GetAssetsForProject_Should_ReturnOkResult_WithFilteredAssets()
        {
            // Arrange
            var projectId = 1;
            var filterOn = "status";
            var filterQuery = "IMPORTED";
            var sortBy = "filename";
            var isAscending = false;
            var pageNumber = 2;
            var pageSize = 10;

            var expectedAssets = new PaginatedResponse<AssetDto>
            {
                Data =
                [
                    new() {
                        Id = 1,
                        ExternalId = "asset-1",
                        Filename = "test1.jpg",
                        Status = AssetStatus.IMPORTED,
                        ProjectId = projectId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                ],
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = 1,
                TotalItems = 1
            };

            _mockAssetService.Setup(s => s.GetAssetsForProjectAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize))
                .ReturnsAsync(expectedAssets);

            // Act
            var result = await _controller.GetAssetsForProject(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var paginatedResponse = Assert.IsType<PaginatedResponse<AssetDto>>(okResult.Value);
            Assert.Single(paginatedResponse.Data);
            Assert.Equal(1, paginatedResponse.TotalItems);

            _mockAssetService.Verify(s => s.GetAssetsForProjectAsync(
                projectId, filterOn, filterQuery, sortBy, isAscending, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetAssetsForProject_Should_ReturnOkResult_WithEmptyList_WhenNoAssetsFound()
        {
            // Arrange
            var projectId = 1;
            var expectedAssets = new PaginatedResponse<AssetDto>
            {
                Data = [],
                PageSize = 25,
                CurrentPage = 1,
                TotalPages = 0,
                TotalItems = 0
            };

            _mockAssetService.Setup(s => s.GetAssetsForProjectAsync(
                projectId, null, null, null, true, 1, 25))
                .ReturnsAsync(expectedAssets);

            // Act
            var result = await _controller.GetAssetsForProject(projectId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var paginatedResponse = Assert.IsType<PaginatedResponse<AssetDto>>(okResult.Value);
            Assert.Empty(paginatedResponse.Data);
            Assert.Equal(0, paginatedResponse.TotalItems);
        }

        [Fact]
        public async Task GetAssetsForProject_Should_ReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var projectId = 1;
            _mockAssetService.Setup(s => s.GetAssetsForProjectAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAssetsForProject(projectId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred. Please try again later.", statusResult.Value);
        }

        #endregion

        #region GetAssetById Tests

        [Fact]
        public async Task GetAssetById_Should_ReturnOkResult_WhenAssetExists()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;
            var expectedAsset = new AssetDto
            {
                Id = assetId,
                ExternalId = "asset-1",
                Filename = "test.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 1024,
                Status = AssetStatus.IMPORTED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProjectId = projectId
            };

            _mockAssetService.Setup(s => s.GetAssetByIdAsync(assetId))
                .ReturnsAsync(expectedAsset);

            // Act
            var result = await _controller.GetAssetById(projectId, assetId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var asset = Assert.IsType<AssetDto>(okResult.Value);
            Assert.Equal(expectedAsset.Id, asset.Id);
            Assert.Equal(expectedAsset.Filename, asset.Filename);

            _mockAssetService.Verify(s => s.GetAssetByIdAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task GetAssetById_Should_ReturnNotFound_WhenAssetDoesNotExist()
        {
            // Arrange
            var projectId = 1;
            var assetId = 999;

            _mockAssetService.Setup(s => s.GetAssetByIdAsync(assetId))
                .ReturnsAsync((AssetDto?)null);

            // Act
            var result = await _controller.GetAssetById(projectId, assetId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Asset with ID {assetId} not found.", notFoundResult.Value);

            _mockAssetService.Verify(s => s.GetAssetByIdAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task GetAssetById_Should_ReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;

            _mockAssetService.Setup(s => s.GetAssetByIdAsync(assetId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAssetById(projectId, assetId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred. Please try again later.", statusResult.Value);
        }

        #endregion

        #region CreateAsset Tests

        [Fact]
        public async Task CreateAsset_Should_ReturnCreatedResult_WhenValidData()
        {
            // Arrange
            var projectId = 1;
            var createAssetDto = new CreateAssetDto
            {
                ExternalId = "new-asset-id",
                Filename = "new-asset.jpg",
                MimeType = "image/jpeg",
                SizeBytes = 1024,
                Width = 800,
                Height = 600
            };

            var createdAsset = new AssetDto
            {
                Id = 1,
                ExternalId = createAssetDto.ExternalId,
                Filename = createAssetDto.Filename,
                MimeType = createAssetDto.MimeType,
                SizeBytes = createAssetDto.SizeBytes,
                Width = createAssetDto.Width,
                Height = createAssetDto.Height,
                Status = AssetStatus.PENDING_IMPORT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ProjectId = projectId
            };

            _mockAssetService.Setup(s => s.CreateAssetAsync(projectId, createAssetDto))
                .ReturnsAsync(createdAsset);

            // Act
            var result = await _controller.CreateAsset(projectId, createAssetDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(AssetsController.GetAssetById), createdResult.ActionName);
            Assert.Equal(projectId, createdResult.RouteValues!["projectId"]);
            Assert.Equal(createdAsset.Id, createdResult.RouteValues["assetId"]);

            var asset = Assert.IsType<AssetDto>(createdResult.Value);
            Assert.Equal(createdAsset.Id, asset.Id);
            Assert.Equal(createdAsset.ExternalId, asset.ExternalId);

            _mockAssetService.Verify(s => s.CreateAssetAsync(projectId, createAssetDto), Times.Once);
        }

        [Fact]
        public async Task CreateAsset_Should_ReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var projectId = 1;
            var createAssetDto = new CreateAssetDto
            {
                ExternalId = "new-asset-id",
                Filename = "new-asset.jpg"
            };

            _mockAssetService.Setup(s => s.CreateAssetAsync(projectId, createAssetDto))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateAsset(projectId, createAssetDto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred. Please try again later.", statusResult.Value);
        }

        #endregion

        #region UpdateAsset Tests

        [Fact]
        public async Task UpdateAsset_Should_ReturnOkResult_WhenAssetUpdatedSuccessfully()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;
            var updateAssetDto = new UpdateAssetDto
            {
                Filename = "updated-asset.jpg",
                MimeType = "image/jpeg",
                Status = AssetStatus.READY_FOR_ANNOTATION,
                SizeBytes = 2048
            };

            var updatedAsset = new AssetDto
            {
                Id = assetId,
                ExternalId = "existing-asset-id",
                Filename = updateAssetDto.Filename,
                MimeType = updateAssetDto.MimeType,
                SizeBytes = updateAssetDto.SizeBytes,
                Status = updateAssetDto.Status,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
                ProjectId = projectId
            };

            _mockAssetService.Setup(s => s.UpdateAssetAsync(assetId, updateAssetDto))
                .ReturnsAsync(updatedAsset);

            // Act
            var result = await _controller.UpdateAsset(projectId, assetId, updateAssetDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var asset = Assert.IsType<AssetDto>(okResult.Value);
            Assert.Equal(updatedAsset.Id, asset.Id);
            Assert.Equal(updatedAsset.Filename, asset.Filename);
            Assert.Equal(updatedAsset.Status, asset.Status);

            _mockAssetService.Verify(s => s.UpdateAssetAsync(assetId, updateAssetDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsset_Should_ReturnNotFound_WhenAssetDoesNotExist()
        {
            // Arrange
            var projectId = 1;
            var assetId = 999;
            var updateAssetDto = new UpdateAssetDto
            {
                Filename = "updated-asset.jpg",
                Status = AssetStatus.READY_FOR_ANNOTATION
            };

            _mockAssetService.Setup(s => s.UpdateAssetAsync(assetId, updateAssetDto))
                .ReturnsAsync((AssetDto?)null);

            // Act
            var result = await _controller.UpdateAsset(projectId, assetId, updateAssetDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Asset with ID {assetId} not found.", notFoundResult.Value);

            _mockAssetService.Verify(s => s.UpdateAssetAsync(assetId, updateAssetDto), Times.Once);
        }

        [Fact]
        public async Task UpdateAsset_Should_ReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;
            var updateAssetDto = new UpdateAssetDto
            {
                Filename = "updated-asset.jpg",
                Status = AssetStatus.READY_FOR_ANNOTATION
            };

            _mockAssetService.Setup(s => s.UpdateAssetAsync(assetId, updateAssetDto))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateAsset(projectId, assetId, updateAssetDto);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred. Please try again later.", statusResult.Value);
        }

        #endregion

        #region DeleteAsset Tests

        [Fact]
        public async Task DeleteAsset_Should_ReturnNoContent_WhenAssetDeletedSuccessfully()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;

            _mockAssetService.Setup(s => s.DeleteAssetAsync(assetId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAsset(projectId, assetId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockAssetService.Verify(s => s.DeleteAssetAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsset_Should_ReturnNotFound_WhenAssetDoesNotExist()
        {
            // Arrange
            var projectId = 1;
            var assetId = 999;

            _mockAssetService.Setup(s => s.DeleteAssetAsync(assetId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAsset(projectId, assetId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal($"Asset with ID {assetId} not found.", notFoundResult.Value);

            _mockAssetService.Verify(s => s.DeleteAssetAsync(assetId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsset_Should_ReturnInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var projectId = 1;
            var assetId = 1;

            _mockAssetService.Setup(s => s.DeleteAssetAsync(assetId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteAsset(projectId, assetId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
            Assert.Equal("An unexpected error occurred. Please try again later.", statusResult.Value);
        }

        #endregion
    }
}
