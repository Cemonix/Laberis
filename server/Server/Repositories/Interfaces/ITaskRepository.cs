using LaberisTask = server.Models.Domain.Task;
using server.Models.Domain;

namespace server.Repositories.Interfaces;

public interface ITaskRepository : IGenericRepository<LaberisTask>
{
}
