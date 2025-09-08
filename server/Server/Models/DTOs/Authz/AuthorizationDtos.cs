namespace server.Models.DTOs.Authz;

public class AuthorizationCheckDto
{
    public required string Permission { get; init; }
    public AuthorizationContextDto? Context { get; init; }
}

public class AuthorizationContextDto
{
    public int? ProjectId { get; init; }
}

public class AuthorizationBatchRequestDto
{
    public required List<AuthorizationCheckDto> Checks { get; init; }
}

public class AuthorizationResponseDto
{
    public required bool Allow { get; init; }
    public string? PolicyVersion { get; init; }
}

public class AuthorizationBatchResponseDto
{
    public required List<bool> Results { get; init; }
    public string? PolicyVersion { get; init; }
}

