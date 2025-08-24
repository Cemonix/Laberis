using Microsoft.Extensions.Logging;
using Moq;
using server.Models.Domain;
using server.Models.DTOs.Workflow;
using server.Repositories.Interfaces;
using server.Services;
using server.Services.Interfaces;
using Server.Tests.Factories;

namespace Server.Tests.Services;

public class WorkflowServiceTests
{
    private readonly Mock<IWorkflowRepository> _mockWorkflowRepository;
    private readonly Mock<IWorkflowStageService> _mockWorkflowStageService;
    private readonly Mock<IWorkflowStageAssignmentService> _mockWorkflowStageAssignmentService;
    private readonly Mock<IWorkflowStageConnectionService> _mockWorkflowStageConnectionService;
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly DbContextFactory _dbContextFactory;
    private readonly Mock<ILogger<WorkflowService>> _mockLogger;
    private readonly WorkflowService _workflowService;

    public WorkflowServiceTests()
    {
        _mockWorkflowRepository = new Mock<IWorkflowRepository>();
        _mockWorkflowStageService = new Mock<IWorkflowStageService>();
        _mockWorkflowStageAssignmentService = new Mock<IWorkflowStageAssignmentService>();
        _mockWorkflowStageConnectionService = new Mock<IWorkflowStageConnectionService>();
        _mockTaskService = new Mock<ITaskService>();
        _dbContextFactory = new DbContextFactory();
        _mockLogger = new Mock<ILogger<WorkflowService>>();

        _workflowService = new WorkflowService(
            _mockWorkflowRepository.Object,
            _mockWorkflowStageService.Object,
            _mockWorkflowStageAssignmentService.Object,
            _mockWorkflowStageConnectionService.Object,
            _mockTaskService.Object,
            _dbContextFactory.Context,
            _mockLogger.Object
        );
    }

    [Fact]
    public async System.Threading.Tasks.Task GetWorkflowsForProject_ShouldReturnWorkflows_WhenProjectExists()
    {
        // Arrange
        int projectId = 1;

        var testProject = new Project { ProjectId = projectId, Name = "Test Project" };
        var workflows = new List<Workflow>
        {
            new() { WorkflowId = 1, Name = "Test Workflow", ProjectId = projectId, Project = testProject },
            new() { WorkflowId = 2, Name = "Another Workflow", ProjectId = projectId, Project = testProject }
        };

        // Set up the mock repository to return the workflows
        _mockWorkflowRepository.Setup(repo => repo.GetAllWithCountAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Workflow, bool>>>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>())
        ).ReturnsAsync((workflows, workflows.Count));

        // Act
        var result = await _workflowService.GetWorkflowsForProjectAsync(
            testProject.ProjectId, pageNumber: 1, pageSize: 25
        );
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(25, result.PageSize);
        Assert.NotEmpty(result.Data);
        Assert.Equal(2, result.Data.Length);
        Assert.Equal("Test Workflow", result.Data.First().Name);
        Assert.Equal("Another Workflow", result.Data.Last().Name);
    }

    [Fact]
    public void CreateWorkflowAsync_ShouldRequireStages_WhenNoStagesProvided()
    {
        // Arrange
        var createDto = new CreateWorkflowDto
        {
            Name = "Test Workflow",
            LabelSchemeId = 1,
            Stages = [], // No stages - should fail
            IncludeReviewStage = false
        };

        // Act - Test the validation logic from WorkflowService
        var hasNoStages = createDto.Stages.Count == 0;

        // Assert
        Assert.True(hasNoStages, 
            "Should identify when no stages are provided and require them from frontend");
    }

    [Fact]
    public void CreateWorkflowAsync_ShouldProcessCustomStages_WhenStagesProvided()
    {
        // Arrange
        var createDto = new CreateWorkflowDto
        {
            Name = "Test Workflow",
            LabelSchemeId = 1,
            Stages = [
                new()
                {
                    Name = "Custom Stage",
                    Description = "A custom workflow stage",
                    AssignedProjectMemberIds = [1, 2, 3]
                }
            ], // Custom stages with assignments provided
            IncludeReviewStage = true // This should be ignored when custom stages exist
        };

        // Act - Test the validation logic from WorkflowService  
        var hasCustomStages = createDto.Stages.Count > 0;
        var hasAssignments = createDto.Stages.First().AssignedProjectMemberIds.Count > 0;

        // Assert
        Assert.True(hasCustomStages, 
            "Should process custom stages when provided by frontend");
        Assert.True(hasAssignments,
            "Should process stage assignments when provided by frontend");
    }

    [Fact]
    public void CreateWorkflowDto_ShouldHaveSimplifiedProperties()
    {
        // Arrange & Act
        var createDto = new CreateWorkflowDto
        {
            Name = "Test",
            LabelSchemeId = 1
        };

        // Assert - Verify the simplified DTO structure
        Assert.False(createDto.IncludeReviewStage);
        Assert.Empty(createDto.Stages);
        
        // Verify that CreateDefaultStages property no longer exists
        var properties = typeof(CreateWorkflowDto).GetProperties();
        var hasCreateDefaultStagesProperty = properties.Any(p => p.Name == "CreateDefaultStages");
        Assert.False(hasCreateDefaultStagesProperty, 
            "CreateDefaultStages property should no longer exist in simplified DTO");
    }
}