using server.Models.Domain;
using server.Models.DTOs.WorkflowStage;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class WorkflowStageConnectionService : IWorkflowStageConnectionService
{
    private readonly IWorkflowStageConnectionRepository _connectionRepository;
    private readonly IWorkflowStageRepository _workflowStageRepository;
    private readonly ILogger<WorkflowStageConnectionService> _logger;

    public WorkflowStageConnectionService(
        IWorkflowStageConnectionRepository connectionRepository,
        IWorkflowStageRepository workflowStageRepository,
        ILogger<WorkflowStageConnectionService> logger)
    {
        _connectionRepository = connectionRepository ?? throw new ArgumentNullException(nameof(connectionRepository));
        _workflowStageRepository = workflowStageRepository ?? throw new ArgumentNullException(nameof(workflowStageRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<WorkflowStageConnectionDto>> GetConnectionsForWorkflowAsync(int workflowId)
    {
        _logger.LogInformation("Fetching connections for workflow: {WorkflowId}", workflowId);

        var connections = await _connectionRepository.GetConnectionsForWorkflowAsync(workflowId);
        return connections.Select(MapToDto);
    }

    public async Task<WorkflowStageConnectionDto?> GetConnectionByIdAsync(int connectionId)
    {
        _logger.LogInformation("Fetching connection with ID: {ConnectionId}", connectionId);
        
        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        return connection != null ? MapToDto(connection) : null;
    }

    public async Task<WorkflowStageConnectionDto> CreateConnectionAsync(CreateWorkflowStageConnectionDto createDto)
    {
        _logger.LogInformation("Creating connection from stage {FromStageId} to stage {ToStageId}", createDto.FromStageId, createDto.ToStageId);

        var connection = new WorkflowStageConnection
        {
            FromStageId = createDto.FromStageId,
            ToStageId = createDto.ToStageId,
            Condition = createDto.Condition,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _connectionRepository.AddAsync(connection);
        await _connectionRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully created connection with ID: {ConnectionId}", connection.WorkflowStageConnectionId);
        
        return MapToDto(connection);
    }

    public async Task<WorkflowStageConnectionDto?> UpdateConnectionAsync(int connectionId, UpdateWorkflowStageConnectionDto updateDto)
    {
        _logger.LogInformation("Updating connection with ID: {ConnectionId}", connectionId);

        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        
        if (connection == null)
        {
            _logger.LogWarning("Connection with ID: {ConnectionId} not found for update.", connectionId);
            return null;
        }

        connection.FromStageId = updateDto.FromStageId;
        connection.ToStageId = updateDto.ToStageId;
        connection.Condition = updateDto.Condition ?? connection.Condition;
        connection.UpdatedAt = DateTime.UtcNow;

        await _connectionRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully updated connection with ID: {ConnectionId}", connectionId);
        
        return MapToDto(connection);
    }

    public async Task<bool> DeleteConnectionAsync(int connectionId)
    {
        _logger.LogInformation("Deleting connection with ID: {ConnectionId}", connectionId);

        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        
        if (connection == null)
        {
            _logger.LogWarning("Connection with ID: {ConnectionId} not found for deletion.", connectionId);
            return false;
        }

        _connectionRepository.Remove(connection);
        await _connectionRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted connection with ID: {ConnectionId}", connectionId);
        
        return true;
    }

    public async System.Threading.Tasks.Task DeleteConnectionsForStageAsync(int stageId)
    {
        _logger.LogInformation("Deleting all connections for stage: {StageId}", stageId);

        await _connectionRepository.DeleteConnectionsForStageAsync(stageId);
        await _connectionRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted all connections for stage: {StageId}", stageId);
    }

    public async Task<IEnumerable<WorkflowStageConnectionDto>> GetIncomingConnectionsAsync(int stageId)
    {
        _logger.LogInformation("Fetching incoming connections for stage: {StageId}", stageId);

        var connections = await _connectionRepository.GetIncomingConnectionsAsync(stageId);
        return connections.Select(MapToDto);
    }

    public async Task<IEnumerable<WorkflowStageConnectionDto>> GetOutgoingConnectionsAsync(int stageId)
    {
        _logger.LogInformation("Fetching outgoing connections for stage: {StageId}", stageId);

        var connections = await _connectionRepository.GetOutgoingConnectionsAsync(stageId);
        return connections.Select(MapToDto);
    }

    public async Task<bool> ValidateConnectionBelongsToWorkflowAsync(int connectionId, int workflowId)
    {
        _logger.LogInformation("Validating if connection {ConnectionId} belongs to workflow {WorkflowId}", connectionId, workflowId);

        var connection = await _connectionRepository.GetByIdAsync(connectionId);
        if (connection == null)
        {
            _logger.LogWarning("Connection with ID {ConnectionId} not found", connectionId);
            return false;
        }

        // Check if both fromStage and toStage belong to the specified workflow
        var fromStage = await _workflowStageRepository.GetByIdAsync(connection.FromStageId);
        var toStage = await _workflowStageRepository.GetByIdAsync(connection.ToStageId);

        if (fromStage == null || toStage == null)
        {
            _logger.LogWarning("One or both stages referenced by connection {ConnectionId} not found", connectionId);
            return false;
        }

        var isValid = fromStage.WorkflowId == workflowId && toStage.WorkflowId == workflowId;
        _logger.LogInformation("Connection {ConnectionId} belongs to workflow {WorkflowId}: {IsValid}", connectionId, workflowId, isValid);
        
        return isValid;
    }

    public async Task<bool> ValidateConnectionStagesBelongToWorkflowAsync(CreateWorkflowStageConnectionDto createDto, int workflowId)
    {
        _logger.LogInformation("Validating if stages in connection DTO belong to workflow {WorkflowId}", workflowId);

        var fromStage = await _workflowStageRepository.GetByIdAsync(createDto.FromStageId);
        var toStage = await _workflowStageRepository.GetByIdAsync(createDto.ToStageId);

        if (fromStage == null)
        {
            _logger.LogWarning("From stage with ID {FromStageId} not found", createDto.FromStageId);
            return false;
        }

        if (toStage == null)
        {
            _logger.LogWarning("To stage with ID {ToStageId} not found", createDto.ToStageId);
            return false;
        }

        var isValid = fromStage.WorkflowId == workflowId && toStage.WorkflowId == workflowId;
        _logger.LogInformation("Connection stages belong to workflow {WorkflowId}: {IsValid}", workflowId, isValid);
        
        return isValid;
    }

    private static WorkflowStageConnectionDto MapToDto(WorkflowStageConnection connection)
    {
        return new WorkflowStageConnectionDto
        {
            Id = connection.WorkflowStageConnectionId,
            FromStageId = connection.FromStageId,
            ToStageId = connection.ToStageId,
            Condition = connection.Condition,
            CreatedAt = connection.CreatedAt,
            UpdatedAt = connection.UpdatedAt
        };
    }
}
