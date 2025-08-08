using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using server.Services;
using server.Services.Interfaces;
using server.Repositories.Interfaces;
using server.Models.DTOs.Task;
using server.Models.Domain;
using LaberisTask = server.Models.Domain.Task;

namespace Server.Tests.Services
{
    public class TaskServiceTests
    {
        private readonly Mock<ITaskRepository> _mockTaskRepository;
        private readonly Mock<ITaskEventRepository> _mockTaskEventRepository;
        private readonly Mock<ITaskEventService> _mockTaskEventService;
        private readonly Mock<ITaskStatusValidator> _mockTaskStatusValidator;
        private readonly Mock<IAssetService> _mockAssetService;
        private readonly Mock<IWorkflowStageRepository> _mockWorkflowStageRepository;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ILogger<TaskService>> _mockLogger;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockTaskEventRepository = new Mock<ITaskEventRepository>();
            _mockTaskEventService = new Mock<ITaskEventService>();
            _mockTaskStatusValidator = new Mock<ITaskStatusValidator>();
            _mockAssetService = new Mock<IAssetService>();
            _mockWorkflowStageRepository = new Mock<IWorkflowStageRepository>();
            _mockUserManager = MockUserManager();
            _mockLogger = new Mock<ILogger<TaskService>>();
            
            _taskService = new TaskService(
                _mockTaskRepository.Object, 
                _mockTaskEventRepository.Object,
                _mockTaskEventService.Object,
                _mockTaskStatusValidator.Object,
                _mockAssetService.Object,
                _mockWorkflowStageRepository.Object,
                _mockUserManager.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTaskByIdAsync_Should_ReturnTaskDto_WhenTaskExists()
        {
            // Arrange
            var taskId = 1;
            var task = new LaberisTask
            {
                TaskId = taskId,
                Priority = 2,
                DueDate = DateTime.UtcNow.AddDays(7),
                ProjectId = 1,
                AssetId = 1,
                WorkflowId = 1,
                CurrentWorkflowStageId = 1,
                AssignedToUserId = "user123",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _taskService.GetTaskByIdAsync(taskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(taskId, result.Id);
            Assert.Equal(task.Priority, result.Priority);
            Assert.Equal(task.ProjectId, result.ProjectId);
            Assert.Equal(task.AssetId, result.AssetId);
            
            _mockTaskRepository.Verify(r => r.GetByIdAsync(taskId), Times.Once);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetTaskByIdAsync_Should_ReturnNull_WhenTaskDoesNotExist()
        {
            // Arrange
            var taskId = 999;
            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId))
                .ReturnsAsync((LaberisTask?)null);

            // Act
            var result = await _taskService.GetTaskByIdAsync(taskId);

            // Assert
            Assert.Null(result);
            _mockTaskRepository.Verify(r => r.GetByIdAsync(taskId), Times.Once);
        }

        [Fact]  
        public async System.Threading.Tasks.Task CreateTaskAsync_Should_ReturnTaskDto_WhenValidData()
        {
            // Arrange
            var projectId = 1;
            var createDto = new CreateTaskDto
            {
                AssetId = 1,
                WorkflowId = 1,
                CurrentWorkflowStageId = 1,
                Priority = 2,
                DueDate = DateTime.UtcNow.AddDays(7),
                AssignedToUserId = "user123"
            };

            _mockTaskRepository.Setup(r => r.AddAsync(It.IsAny<LaberisTask>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _taskService.CreateTaskAsync(projectId, createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Priority, result.Priority);
            Assert.Equal(createDto.AssetId, result.AssetId);
            Assert.Equal(createDto.WorkflowId, result.WorkflowId);
            Assert.Equal(projectId, result.ProjectId);
            
            _mockTaskRepository.Verify(r => r.AddAsync(It.Is<LaberisTask>(t => 
                t.Priority == createDto.Priority &&
                t.AssetId == createDto.AssetId &&
                t.ProjectId == projectId)), Times.Once);
            _mockTaskRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }
    }
}
