namespace server.Models.DTOs;

public record class PaginatedResponse<T>
{
    public T[] Data { get; init; } = [];
    public int PageSize { get; init; }
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
}
