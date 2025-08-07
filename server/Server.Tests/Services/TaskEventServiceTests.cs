using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Models.Domain.Enums;
using server.Models.DTOs.TaskEvent;
using server.Repositories.Interfaces;
using server.Services;
using Xunit;
using TaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace Server.Tests.Services;

public class TaskEventServiceTests
{
    private readonly Mock<ITaskEventRepository> _mockTaskEventRepository;
    private readonly Mock<ILogger<TaskEventService>> _mockLogger;
    private readonly TaskEventService _taskEventService;

    public TaskEventServiceTests()
    {
        _mockTaskEventRepository = new Mock<ITaskEventRepository>();
        _mockLogger = new Mock<ILogger<TaskEventService>>();
        _taskEventService = new TaskEventService(_mockTaskEventRepository.Object, _mockLogger.Object);
    }

    [Theory]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.SUSPENDED, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.IN_PROGRESS, TaskStatus.DEFERRED, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.ARCHIVED, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.SUSPENDED, TaskStatus.IN_PROGRESS, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.READY_FOR_ANNOTATION, TaskStatus.IN_PROGRESS, TaskEventType.STATUS_CHANGED)]
    [InlineData(TaskStatus.COMPLETED, TaskStatus.READY_FOR_ANNOTATION, TaskEventType.STATUS_CHANGED)]
    public async System.Threading.Tasks.Task LogStatusChangeEventAsync_ShouldCreateCorrectEventType(
        TaskStatus fromStatus, TaskStatus toStatus, TaskEventType expectedEventType)
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var expectedTaskEvent = new TaskEvent
        {
            EventId = 1,
            EventType = expectedEventType,
            Details = $"Task status changed from {fromStatus} to {toStatus}",
            TaskId = taskId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _mockTaskEventRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskEvent>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        _mockTaskEventRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _taskEventService.LogStatusChangeEventAsync(taskId, fromStatus, toStatus, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEventType, result.EventType);
        Assert.Contains(fromStatus.ToString(), result.Details);
        Assert.Contains(toStatus.ToString(), result.Details);

        _mockTaskEventRepository.Verify(x => x.AddAsync(It.Is<TaskEvent>(te => 
            te.EventType == expectedEventType &&
            te.TaskId == taskId &&
            te.UserId == userId &&
            te.Details != null &&
            te.Details.Contains(fromStatus.ToString()) &&
            te.Details.Contains(toStatus.ToString())
        )), Times.Once);

        _mockTaskEventRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task LogStatusChangeEventAsync_ShouldSetCorrectTimestamp()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var beforeCall = DateTime.UtcNow;

        _mockTaskEventRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskEvent>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        _mockTaskEventRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _taskEventService.LogStatusChangeEventAsync(taskId, TaskStatus.IN_PROGRESS, TaskStatus.COMPLETED, userId);
        var afterCall = DateTime.UtcNow;

        // Assert
        _mockTaskEventRepository.Verify(x => x.AddAsync(It.Is<TaskEvent>(te => 
            te.CreatedAt >= beforeCall && te.CreatedAt <= afterCall
        )), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task LogStatusChangeEventAsync_ShouldHandleUnknownStatusTransition()
    {
        // Arrange
        const int taskId = 1;
        const string userId = "user123";
        var unknownFromStatus = (TaskStatus)998;
        var unknownToStatus = (TaskStatus)999;

        _mockTaskEventRepository
            .Setup(x => x.AddAsync(It.IsAny<TaskEvent>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        _mockTaskEventRepository
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _taskEventService.LogStatusChangeEventAsync(taskId, unknownFromStatus, unknownToStatus, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskEventType.STATUS_CHANGED, result.EventType); // Should default to STATUS_CHANGED
        
        _mockTaskEventRepository.Verify(x => x.AddAsync(It.Is<TaskEvent>(te => 
            te.EventType == TaskEventType.STATUS_CHANGED
        )), Times.Once);
    }
}