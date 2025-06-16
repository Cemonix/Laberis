using server.Models.Domain;
using server.Models.DTOs.TaskEvent;
using server.Models.Common;
using server.Repositories.Interfaces;
using server.Services.Interfaces;

namespace server.Services;

public class TaskEventService : ITaskEventService
{
    private readonly ITaskEventRepository _taskEventRepository;
    private readonly ILogger<TaskEventService> _logger;

    public TaskEventService(
        ITaskEventRepository taskEventRepository,
        ILogger<TaskEventService> logger)
    {
        _taskEventRepository = taskEventRepository ?? throw new ArgumentNullException(nameof(taskEventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResponse<TaskEventDto>> GetTaskEventsAsync(
        int taskId,
        string? filterOn = null, string? filterQuery = null, string? sortBy = null,
        bool isAscending = true, int pageNumber = 1, int pageSize = 25)
    {
        _logger.LogInformation("Fetching task events for task: {TaskId}", taskId);

        var (events, totalCount) = await _taskEventRepository.GetAllWithCountAsync(
            filter: te => te.TaskId == taskId,
            filterOn: filterOn,
            filterQuery: filterQuery,
            sortBy: sortBy ?? "created_at",
            isAscending: isAscending,
            pageNumber: pageNumber,
            pageSize: pageSize
        );

        _logger.LogInformation("Fetched {Count} task events for task: {TaskId}", events.Count(), taskId);

        var eventDtos = events.Select(MapToDto).ToArray();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<TaskEventDto>
        {
            Data = eventDtos,
            PageSize = pageSize,
            CurrentPage = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalCount
        };
    }

    public async Task<TaskEventDto?> GetTaskEventByIdAsync(long eventId)
    {
        _logger.LogInformation("Fetching task event with ID: {EventId}", eventId);

        var taskEvent = await _taskEventRepository.GetByIdAsync(eventId);

        if (taskEvent == null)
        {
            _logger.LogWarning("Task event with ID: {EventId} not found.", eventId);
            return null;
        }

        return MapToDto(taskEvent);
    }

    public async Task<TaskEventDto> LogTaskEventAsync(CreateTaskEventDto createDto, string? userId = null)
    {
        _logger.LogInformation("Logging new task event for task: {TaskId}, event type: {EventType}", createDto.TaskId, createDto.EventType);

        var taskEvent = new TaskEvent
        {
            EventType = createDto.EventType,
            Details = createDto.Details,
            TaskId = createDto.TaskId,
            UserId = userId,
            FromWorkflowStageId = createDto.FromWorkflowStageId,
            ToWorkflowStageId = createDto.ToWorkflowStageId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskEventRepository.AddAsync(taskEvent);
        await _taskEventRepository.SaveChangesAsync();

        _logger.LogInformation("Successfully logged task event with ID: {EventId}", taskEvent.EventId);

        return MapToDto(taskEvent);
    }

    private static TaskEventDto MapToDto(TaskEvent taskEvent)
    {
        return new TaskEventDto
        {
            Id = taskEvent.EventId,
            EventType = taskEvent.EventType,
            Details = taskEvent.Details,
            CreatedAt = taskEvent.CreatedAt,
            TaskId = taskEvent.TaskId,
            UserId = taskEvent.UserId,
            FromWorkflowStageId = taskEvent.FromWorkflowStageId,
            ToWorkflowStageId = taskEvent.ToWorkflowStageId
        };
    }
}
