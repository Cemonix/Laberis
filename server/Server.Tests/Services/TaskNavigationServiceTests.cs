using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Services;
using server.Services.Interfaces;
using server.Repositories.Interfaces;
using Xunit;
using DomainTask = server.Models.Domain.Task;
using DomainTaskStatus = server.Models.Domain.Enums.TaskStatus;

namespace Server.Tests.Services;

public class TaskNavigationServiceTests
{
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<ILogger<TaskNavigationService>> _mockLogger;
    private readonly TaskNavigationService _service;

    public TaskNavigationServiceTests()
    {
        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockLogger = new Mock<ILogger<TaskNavigationService>>();
        _service = new TaskNavigationService(_mockTaskRepository.Object, _mockLogger.Object);
    }

    #region Helper Methods

    private List<DomainTask> GetTestNavigableTasks(string userId = "test-user", int projectId = 1, int workflowStageId = 10)
    {
        return
        [
            new() {
                TaskId = 1,
                AssignedToUserId = userId,
                ProjectId = projectId,
                AssetId = 101,
                WorkflowStageId = workflowStageId,
                Status = DomainTaskStatus.READY_FOR_ANNOTATION,
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                RowVersion = []
            },
            new() {
                TaskId = 2,
                AssignedToUserId = userId,
                ProjectId = projectId,
                AssetId = 102,
                WorkflowStageId = workflowStageId,
                Status = DomainTaskStatus.IN_PROGRESS,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                RowVersion = []
            },
            new() {
                TaskId = 3,
                AssignedToUserId = userId,
                ProjectId = projectId,
                AssetId = 103,
                WorkflowStageId = workflowStageId,
                Status = DomainTaskStatus.READY_FOR_ANNOTATION,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                RowVersion = []
            }
        ];
    }

    #endregion

    #region GetNextTaskAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_WithValidCurrentTask_ReturnsNextTask()
    {
        // Arrange
        var tasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 1; // First task
        int workflowStageId = 10;

        var currentTask = tasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Set up task repository to allow auto-assignment updates
        _mockTaskRepository.Setup(x => x.Update(It.IsAny<DomainTask>()));
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Equal(2, result.TaskId); // Next task ID
        Assert.Equal(102, result.AssetId); // Next asset ID
        Assert.True(result.HasNext); // Task 3 is still available
        Assert.True(result.HasPrevious); // Task 1 is behind
        Assert.Equal(1, result.CurrentPosition); // Position of task 1 in navigable list (1-based)
        Assert.Equal(3, result.TotalTasks); // Only tasks 1, 2, 3 are navigable
        Assert.Null(result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_FromLastTask_ReturnsNoNextTask()
    {
        // Arrange
        var tasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 3; // Last navigable task
        int workflowStageId = 10;

        var currentTask = tasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Null(result.TaskId);
        Assert.Null(result.AssetId);
        Assert.False(result.HasNext);
        Assert.True(result.HasPrevious);
        Assert.Equal(3, result.CurrentPosition); // Position of task 3
        Assert.Equal(3, result.TotalTasks);
        Assert.Equal("No more tasks available", result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_WithNonExistentTask_ReturnsEmptyResult()
    {
        // Arrange
        string userId = "test-user";
        int currentTaskId = 999; // Non-existent task

        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync((DomainTask?)null);

        // Act
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, null);

        // Assert
        Assert.Null(result.TaskId);
        Assert.Null(result.AssetId);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
        Assert.Equal(0, result.CurrentPosition);
        Assert.Equal(0, result.TotalTasks);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_UserWithNoTasks_ReturnsAppropriateMessage()
    {
        // Arrange
        string userId = "user-with-no-tasks";
        int currentTaskId = 1;
        var currentTask = new DomainTask { TaskId = currentTaskId, ProjectId = 1, RowVersion = new byte[0] };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, null))
            .ReturnsAsync(new List<DomainTask>());

        // Act
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, null);

        // Assert
        Assert.Null(result.TaskId);
        Assert.Equal(0, result.CurrentPosition);
        Assert.Equal(0, result.TotalTasks);
        Assert.Equal("Current task is not in your assigned task list", result.Message);
    }

    #endregion

    #region GetPreviousTaskAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetPreviousTaskAsync_WithValidCurrentTask_ReturnsPreviousTask()
    {
        // Arrange
        var tasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 2; // Middle task
        int workflowStageId = 10;
        
        var currentTask = tasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Set up task repository to allow auto-assignment updates
        _mockTaskRepository.Setup(x => x.Update(It.IsAny<DomainTask>()));
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.GetPreviousTaskAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Equal(1, result.TaskId); // Previous task ID
        Assert.Equal(101, result.AssetId); // Previous asset ID
        Assert.True(result.HasNext); // Task 3 is still ahead
        Assert.False(result.HasPrevious); // Task 1 is the first
        Assert.Equal(2, result.CurrentPosition); // Position of task 2 in navigable list
        Assert.Equal(3, result.TotalTasks);
        Assert.Null(result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetPreviousTaskAsync_FromFirstTask_ReturnsNoPreviousTask()
    {
        // Arrange
        var tasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 1; // First task
        int workflowStageId = 10;

        var currentTask = tasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetPreviousTaskAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Null(result.TaskId);
        Assert.Null(result.AssetId);
        Assert.True(result.HasNext);
        Assert.False(result.HasPrevious);
        Assert.Equal(1, result.CurrentPosition);
        Assert.Equal(3, result.TotalTasks);
        Assert.Equal("No previous tasks available", result.Message);
    }

    #endregion

    #region GetNavigationContextAsync Tests

    [Fact]
    public async System.Threading.Tasks.Task GetNavigationContextAsync_WithCurrentTaskInList_ReturnsCorrectContext()
    {
        // Arrange
        var tasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 2; // Middle task
        int workflowStageId = 10;

        var currentTask = tasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetNavigationContextAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Equal(currentTaskId, result.TaskId);
        Assert.Equal(102, result.AssetId); // Asset ID of task 2
        Assert.True(result.HasNext);
        Assert.True(result.HasPrevious);
        Assert.Equal(2, result.CurrentPosition); // Task 2 is at position 2
        Assert.Equal(3, result.TotalTasks);
        Assert.Null(result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNavigationContextAsync_WithCompletedTaskNotInList_ReturnsPreviewModeContext()
    {
        // Arrange
        var navigableTasks = GetTestNavigableTasks();
        string userId = "test-user";
        int currentTaskId = 4; // Completed task (not in navigable list)
        var currentTask = new DomainTask
        {
            TaskId = currentTaskId,
            AssignedToUserId = userId,
            ProjectId = 1,
            AssetId = 104,
            WorkflowStageId = 10,
            Status = DomainTaskStatus.COMPLETED,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[0]
        };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, currentTask.ProjectId, null))
            .ReturnsAsync(navigableTasks);

        // Act
        var result = await _service.GetNavigationContextAsync(userId, currentTaskId, null);

        // Assert
        Assert.Equal(0, result.CurrentPosition);
        Assert.Equal(3, result.TotalTasks); // Still 3 navigable tasks available
        Assert.True(result.HasNext); // User has navigable tasks available
        Assert.False(result.HasPrevious);
        Assert.Equal("Viewing task in preview mode", result.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNavigationContextAsync_WithSingleTask_ReturnsCorrectNavigation()
    {
        // Arrange
        string userId = "single-user";
        int projectId = 1;
        int workflowStageId = 10;
        var singleTask = new DomainTask
        {
            TaskId = 100,
            AssignedToUserId = userId,
            ProjectId = projectId,
            AssetId = 200,
            WorkflowStageId = workflowStageId,
            Status = DomainTaskStatus.READY_FOR_ANNOTATION,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[0]
        };
        var tasks = new List<DomainTask> { singleTask };

        _mockTaskRepository.Setup(x => x.GetByIdAsync(singleTask.TaskId))
            .ReturnsAsync(singleTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, projectId, workflowStageId))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetNavigationContextAsync(userId, singleTask.TaskId, workflowStageId);

        // Assert
        Assert.Equal(100, result.TaskId);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
        Assert.Equal(1, result.CurrentPosition);
        Assert.Equal(1, result.TotalTasks);
    }

    #endregion

    #region Workflow Stage Filtering Tests

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_WithWorkflowStageFilter_OnlyReturnsTasksFromThatStage()
    {
        // Arrange
        string userId = "test-user";
        int projectId = 1;
        int currentTaskId = 1;
        int workflowStageId10 = 10;
        int workflowStageId20 = 20;

        var tasksStage10 = GetTestNavigableTasks(userId, projectId, workflowStageId10);
        var tasksStage20 = new List<DomainTask>
        {
            new DomainTask
            {
                TaskId = 6,
                AssignedToUserId = userId,
                ProjectId = projectId,
                AssetId = 106,
                WorkflowStageId = workflowStageId20, // Different stage
                Status = DomainTaskStatus.READY_FOR_ANNOTATION,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[0]
            }
        };

        var currentTask = tasksStage10.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, projectId, workflowStageId10))
            .ReturnsAsync(tasksStage10);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, projectId, workflowStageId20))
            .ReturnsAsync(tasksStage20);

        // Set up task repository to allow auto-assignment updates
        _mockTaskRepository.Setup(x => x.Update(It.IsAny<DomainTask>()));
        _mockTaskRepository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act - Get next task from stage 10 only
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, workflowStageId10);

        // Assert
        Assert.Equal(2, result.TaskId); // Should be task 2 from stage 10, not task 6 from stage 20
        Assert.Equal(3, result.TotalTasks); // Only 3 navigable tasks in stage 10
    }

    [Fact]
    public async System.Threading.Tasks.Task GetNextTaskAsync_WithoutWorkflowStageFilter_ReturnsTasksFromAllStages()
    {
        // Arrange
        string userId = "test-user";
        int projectId = 1;
        int currentTaskId = 1;
        
        var tasksStage10 = GetTestNavigableTasks(userId, projectId, 10);
        var tasksStage20 = new List<DomainTask>
        {
            new DomainTask
            {
                TaskId = 6,
                AssignedToUserId = userId,
                ProjectId = projectId,
                AssetId = 106,
                WorkflowStageId = 20,
                Status = DomainTaskStatus.READY_FOR_ANNOTATION,
                CreatedAt = DateTime.UtcNow.AddMinutes(5),
                RowVersion = new byte[0] // Later creation time
            }
        };
        var allTasks = tasksStage10.Concat(tasksStage20).OrderBy(t => t.CreatedAt).ToList();

        var currentTask = tasksStage10.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, projectId, null))
            .ReturnsAsync(allTasks);

        // Act - No workflow stage filter
        var result = await _service.GetNextTaskAsync(userId, currentTaskId, null);

        // Assert
        Assert.Equal(4, result.TotalTasks); // 4 navigable tasks across all stages
    }

    #endregion

    #region Status Filtering Tests

    [Fact]
    public async System.Threading.Tasks.Task GetNavigationContextAsync_ExcludesCompletedArchivedVetoedTasks()
    {
        // Arrange
        string userId = "test-user";
        int projectId = 1;
        int workflowStageId = 10;
        int currentTaskId = 1;

        var allTasks = new List<DomainTask>
        {
            new() { 
                TaskId = 1, 
                AssignedToUserId = userId, 
                ProjectId = projectId, 
                AssetId = 101, 
                WorkflowStageId = workflowStageId, 
                Status = DomainTaskStatus.READY_FOR_ANNOTATION, 
                CreatedAt = DateTime.UtcNow.AddHours(-4), 
                RowVersion = [] 
            },
            new() { 
                TaskId = 2, 
                AssignedToUserId = userId, 
                ProjectId = projectId, 
                AssetId = 102, 
                WorkflowStageId = workflowStageId, 
                Status = DomainTaskStatus.IN_PROGRESS, 
                CreatedAt = DateTime.UtcNow.AddHours(-3), 
                RowVersion = [] 
            },
            new() { 
                TaskId = 3, 
                AssignedToUserId = userId, 
                ProjectId = projectId, 
                AssetId = 103, 
                WorkflowStageId = workflowStageId, 
                Status = DomainTaskStatus.COMPLETED, 
                CreatedAt = DateTime.UtcNow.AddHours(-2), 
                RowVersion = [] 
            }, // Excluded
            new() { 
                TaskId = 4, 
                AssignedToUserId = userId, 
                ProjectId = projectId, 
                AssetId = 104, 
                WorkflowStageId = workflowStageId, 
                Status = DomainTaskStatus.ARCHIVED, 
                CreatedAt = DateTime.UtcNow.AddHours(-1), 
                RowVersion = [] 
            }, // Excluded
            new() { 
                TaskId = 5, 
                AssignedToUserId = userId, 
                ProjectId = projectId, 
                AssetId = 105, 
                WorkflowStageId = workflowStageId, 
                Status = DomainTaskStatus.VETOED, 
                CreatedAt = DateTime.UtcNow, 
                RowVersion = [] 
            } // Excluded
        };
        
        var navigableTasks = allTasks
            .Where(t => t.Status == DomainTaskStatus.READY_FOR_ANNOTATION || t.Status == DomainTaskStatus.IN_PROGRESS)
            .ToList();

        var currentTask = allTasks.First(t => t.TaskId == currentTaskId);
        _mockTaskRepository.Setup(x => x.GetByIdAsync(currentTaskId))
            .ReturnsAsync(currentTask);
        _mockTaskRepository.Setup(x => x.GetNavigableTasksAsync(userId, projectId, workflowStageId))
            .ReturnsAsync(navigableTasks);

        // Act
        var result = await _service.GetNavigationContextAsync(userId, currentTaskId, workflowStageId);

        // Assert
        Assert.Equal(2, result.TotalTasks); // Only tasks 1 and 2 are navigable
        Assert.Equal(1, result.CurrentPosition);
        Assert.True(result.HasNext); // Task 2 is available
        Assert.False(result.HasPrevious);
    }

    #endregion
}