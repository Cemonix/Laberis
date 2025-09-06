using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Repositories.Interfaces;
using server.Services;
using System.Linq.Expressions;
using Xunit;
using LaberisTask = server.Models.Domain.Task;

namespace Server.Tests.Services;

public class TaskLockingServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ILogger<TaskLockingService>> _mockLogger;
    private readonly TaskLockingService _service;

    public TaskLockingServiceTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockLogger = new Mock<ILogger<TaskLockingService>>();
        _service = new TaskLockingService(_mockTaskRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task TryLockTaskAsync_WithUnlockedTask_ShouldLockSuccessfully()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = new LaberisTask
        {
            TaskId = taskId,
            LockedByUserId = null,
            RowVersion = []
        };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _service.TryLockTaskAsync(taskId, userId);

        // Assert
        Assert.True(result);
        Assert.Equal(userId, task.LockedByUserId);
        Assert.NotNull(task.LockedAt);
        Assert.NotNull(task.LockExpiresAt);
        _mockTaskRepository.Verify(x => x.Update(task), Times.Once);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task TryLockTaskAsync_WithTaskLockedByOtherUser_ShouldFail()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var otherUserId = "user456";
        var task = new LaberisTask
        {
            TaskId = taskId,
            LockedByUserId = otherUserId,
            LockExpiresAt = DateTime.UtcNow.AddMinutes(25),
            RowVersion = []
        };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _service.TryLockTaskAsync(taskId, userId);

        // Assert
        Assert.False(result);
        _mockTaskRepository.Verify(x => x.Update(It.IsAny<LaberisTask>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task TryLockTaskAsync_WithConcurrencyConflict_ShouldFail()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = new LaberisTask { TaskId = taskId, LockedByUserId = null, RowVersion = new byte[0] };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId)).ReturnsAsync(task);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _service.TryLockTaskAsync(taskId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReleaseLockAsync_WithOwnedLock_ShouldReleaseSuccessfully()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = new LaberisTask
        {
            TaskId = taskId,
            LockedByUserId = userId,
            LockExpiresAt = DateTime.UtcNow.AddMinutes(25),
            RowVersion = []
        };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId))
            .ReturnsAsync(task);

        // Act
        var result = await _service.ReleaseLockAsync(taskId, userId);

        // Assert
        Assert.True(result);
        Assert.Null(task.LockedByUserId);
        Assert.Null(task.LockedAt);
        Assert.Null(task.LockExpiresAt);
        _mockTaskRepository.Verify(x => x.Update(task), Times.Once);
        _mockTaskRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReleaseLockAsync_WithConcurrencyConflict_ShouldFail()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = new LaberisTask { TaskId = taskId, LockedByUserId = userId, RowVersion = new byte[0] };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId)).ReturnsAsync(task);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _service.ReleaseLockAsync(taskId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task ExtendLockAsync_WithConcurrencyConflict_ShouldFail()
    {
        // Arrange
        var taskId = 1;
        var userId = "user123";
        var task = new LaberisTask
        {
            TaskId = taskId,
            LockedByUserId = userId,
            LockExpiresAt = DateTime.UtcNow.AddMinutes(10),
            RowVersion = new byte[0]
        };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(taskId)).ReturnsAsync(task);
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _service.ExtendLockAsync(taskId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task CleanupExpiredLocksAsync_WithConcurrencyConflict_ShouldReturnZero()
    {
        // Arrange
        var expiredTasks = new List<LaberisTask>
        {
            new() { TaskId = 1, LockedByUserId = "user1", LockExpiresAt = DateTime.UtcNow.AddMinutes(-10), RowVersion = [] },
        };

        _mockTaskRepository.Setup(x => x.GetAllAsync(
            It.IsAny<Expression<Func<LaberisTask, bool>>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(expiredTasks);

        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _service.CleanupExpiredLocksAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async System.Threading.Tasks.Task ReleaseAllUserLocksAsync_WithConcurrencyConflict_ShouldReturnZero()
    {
        // Arrange
        var userId = "user123";
        var userTasks = new List<LaberisTask>
        {
            new() { TaskId = 1, LockedByUserId = userId, RowVersion = [] }
        };

        _mockTaskRepository.Setup(x => x.GetAllAsync(
            It.IsAny<Expression<Func<LaberisTask, bool>>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        )).ReturnsAsync(userTasks);
        
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _service.ReleaseAllUserLocksAsync(userId);

        // Assert
        Assert.Equal(0, result);
    }
}
