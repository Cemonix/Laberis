using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using server.Controllers;
using server.Models.DTOs.Task;
using server.Services.Interfaces;
using System.Security.Claims;
using Xunit;

namespace Server.Tests.Controllers;

public class TaskNavigationControllerTests
{
    private readonly Mock<ITaskNavigationService> _mockTaskNavigationService;
    private readonly Mock<ILogger<TaskNavigationController>> _mockLogger;
    private readonly TaskNavigationController _controller;

    public TaskNavigationControllerTests()
    {
        _mockTaskNavigationService = new Mock<ITaskNavigationService>();
        _mockLogger = new Mock<ILogger<TaskNavigationController>>();
        _controller = new TaskNavigationController(_mockTaskNavigationService.Object, _mockLogger.Object);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    #region GetNextTask Tests

    [Fact]
    public async Task GetNextTask_WithValidParameters_ReturnsOkWithNavigationResult()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;
        int workflowStageId = 10;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = 101,
            AssetId = 200,
            HasNext = true,
            HasPrevious = true,
            CurrentPosition = 2,
            TotalTasks = 5,
            Message = null
        };

        _mockTaskNavigationService
            .Setup(x => x.GetNextTaskAsync("test-user-id", currentTaskId, workflowStageId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNextTask(projectId, currentTaskId, workflowStageId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var navigationResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Equal(expectedResult.TaskId, navigationResult.TaskId);
        Assert.Equal(expectedResult.AssetId, navigationResult.AssetId);
        Assert.Equal(expectedResult.HasNext, navigationResult.HasNext);
        Assert.Equal(expectedResult.HasPrevious, navigationResult.HasPrevious);
        Assert.Equal(expectedResult.CurrentPosition, navigationResult.CurrentPosition);
        Assert.Equal(expectedResult.TotalTasks, navigationResult.TotalTasks);

        _mockTaskNavigationService.Verify(x => x.GetNextTaskAsync("test-user-id", currentTaskId, workflowStageId), Times.Once);
    }

    [Fact]
    public async Task GetNextTask_WithoutWorkflowStageId_ReturnsOkWithNavigationResult()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = null,
            AssetId = null,
            HasNext = false,
            HasPrevious = true,
            CurrentPosition = 3,
            TotalTasks = 3,
            Message = "No more tasks available"
        };

        _mockTaskNavigationService
            .Setup(x => x.GetNextTaskAsync("test-user-id", currentTaskId, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNextTask(projectId, currentTaskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var navigationResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Null(navigationResult.TaskId);
        Assert.False(navigationResult.HasNext);
        Assert.Equal("No more tasks available", navigationResult.Message);

        _mockTaskNavigationService.Verify(x => x.GetNextTaskAsync("test-user-id", currentTaskId, null), Times.Once);
    }

    [Fact]
    public async Task GetNextTask_WithoutUserClaim_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _controller.GetNextTask(1, 100);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("User ID claim not found in token.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetNextTask_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;

        _mockTaskNavigationService
            .Setup(x => x.GetNextTaskAsync("test-user-id", currentTaskId, null))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetNextTask(projectId, currentTaskId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        Assert.Equal("An unexpected error occurred while getting next task.", statusCodeResult.Value);
    }

    #endregion

    #region GetPreviousTask Tests

    [Fact]
    public async Task GetPreviousTask_WithValidParameters_ReturnsOkWithNavigationResult()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;
        int workflowStageId = 10;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = 99,
            AssetId = 199,
            HasNext = true,
            HasPrevious = true,
            CurrentPosition = 1,
            TotalTasks = 5,
            Message = null
        };

        _mockTaskNavigationService
            .Setup(x => x.GetPreviousTaskAsync("test-user-id", currentTaskId, workflowStageId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPreviousTask(projectId, currentTaskId, workflowStageId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var navigationResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Equal(expectedResult.TaskId, navigationResult.TaskId);
        Assert.Equal(expectedResult.HasPrevious, navigationResult.HasPrevious);

        _mockTaskNavigationService.Verify(x => x.GetPreviousTaskAsync("test-user-id", currentTaskId, workflowStageId), Times.Once);
    }

    [Fact]
    public async Task GetPreviousTask_NoMoreTasks_ReturnsOkWithMessage()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = null,
            AssetId = null,
            HasNext = false,
            HasPrevious = false,
            CurrentPosition = 1,
            TotalTasks = 1,
            Message = "No previous tasks available"
        };

        _mockTaskNavigationService
            .Setup(x => x.GetPreviousTaskAsync("test-user-id", currentTaskId, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPreviousTask(projectId, currentTaskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var navigationResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Null(navigationResult.TaskId);
        Assert.False(navigationResult.HasPrevious);
        Assert.Equal("No previous tasks available", navigationResult.Message);
    }

    #endregion

    #region GetNavigationContext Tests

    [Fact]
    public async Task GetNavigationContext_WithValidParameters_ReturnsOkWithContextResult()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;
        int workflowStageId = 10;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = 100,
            AssetId = 200,
            HasNext = true,
            HasPrevious = true,
            CurrentPosition = 3,
            TotalTasks = 7,
            Message = null
        };

        _mockTaskNavigationService
            .Setup(x => x.GetNavigationContextAsync("test-user-id", currentTaskId, workflowStageId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNavigationContext(projectId, currentTaskId, workflowStageId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var contextResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Equal(expectedResult.CurrentPosition, contextResult.CurrentPosition);
        Assert.Equal(expectedResult.TotalTasks, contextResult.TotalTasks);
        Assert.True(contextResult.HasNext);
        Assert.True(contextResult.HasPrevious);

        _mockTaskNavigationService.Verify(x => x.GetNavigationContextAsync("test-user-id", currentTaskId, workflowStageId), Times.Once);
    }

    [Fact]
    public async Task GetNavigationContext_CompletedTaskPreviewMode_ReturnsOkWithPreviewContext()
    {
        // Arrange
        int projectId = 1;
        int currentTaskId = 100;

        var expectedResult = new TaskNavigationDto
        {
            TaskId = null,
            AssetId = null,
            HasNext = true,
            HasPrevious = false,
            CurrentPosition = 0,
            TotalTasks = 3,
            Message = "Viewing task in preview mode"
        };

        _mockTaskNavigationService
            .Setup(x => x.GetNavigationContextAsync("test-user-id", currentTaskId, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetNavigationContext(projectId, currentTaskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var contextResult = Assert.IsType<TaskNavigationDto>(okResult.Value);
        
        Assert.Equal(0, contextResult.CurrentPosition);
        Assert.Equal("Viewing task in preview mode", contextResult.Message);
    }

    #endregion
}