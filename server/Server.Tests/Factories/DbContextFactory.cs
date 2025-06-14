using Microsoft.EntityFrameworkCore;
using server.Data;
using System;

namespace Server.Tests.Factories
{
    /// <summary>
    /// A factory for creating instances of LaberisDbContext for testing purposes.
    /// It uses the EF Core In-Memory provider to ensure tests are fast and isolated.
    /// </summary>
    public class DbContextFactory : IDisposable
    {
        private readonly DbContextOptions<LaberisDbContext> _options;
        private readonly LaberisDbContext _context;

        public DbContextFactory()
        {
            _options = new DbContextOptionsBuilder<LaberisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new LaberisDbContext(_options);
            _context.Database.EnsureCreated();
        }

        /// <summary>
        /// Provides access to the created DbContext instance.
        /// </summary>
        public LaberisDbContext Context => _context;

        /// <summary>
        /// Disposes of the DbContext to clean up resources.
        /// </summary>
        public void Dispose()
        {
            if (_context != null)
            {
                _context.Database.EnsureDeleted();
                _context.Dispose();
            }
            GC.SuppressFinalize(this);
            
        }
    }
}
