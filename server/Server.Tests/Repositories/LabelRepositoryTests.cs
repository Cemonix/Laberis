using Moq;
using Microsoft.Extensions.Logging;
using Server.Tests.Factories;
using server.Repositories;
using server.Models.Domain;

namespace Server.Tests.Repositories
{
    public class LabelRepositoryTests : IDisposable
    {
        private readonly DbContextFactory _dbContextFactory;
        private readonly ILogger<LabelRepository> _mockLogger;

        public LabelRepositoryTests()
        {
            // Set up the in-memory database factory for each test run.
            _dbContextFactory = new DbContextFactory();
            
            _mockLogger = Mock.Of<ILogger<LabelRepository>>();
        }

        /// <summary>
        /// This test verifies that the AddAsync method correctly saves a new Label entity
        /// to the database.
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task AddAsync_Should_AddLabelToDatabase_WhenCalled()
        {
            var context = _dbContextFactory.Context;
            
            var labelRepository = new LabelRepository(context, _mockLogger);

            // Seed the required related data - a Label needs a LabelScheme, which in turn needs a Project.
            var testProject = new Project { Name = "Test Project" };
            var testLabelScheme = new LabelScheme 
            { 
                Name = "Test Scheme", 
                Project = testProject 
            };
            context.Projects.Add(testProject);
            context.LabelSchemes.Add(testLabelScheme);
            await context.SaveChangesAsync();
            
            var newLabel = new Label
            {
                Name = "Test Label",
                Color = "#FF0000",
                Description = "A label for testing",
                LabelSchemeId = 1 // Assuming a LabelScheme with Id 1 exists for this test context
            };

            await labelRepository.AddAsync(newLabel);
            await labelRepository.SaveChangesAsync();

            // Assert
            Assert.Equal(1, context.Labels.Count());

            var savedLabel = context.Labels.Single();
            Assert.Equal("Test Label", savedLabel.Name);
            Assert.Equal("#FF0000", savedLabel.Color);
            Assert.NotNull(savedLabel);
        }

        public void Dispose()
        {
            // Clean up the in-memory database after each test run.
            _dbContextFactory.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
