using Microsoft.AspNetCore.Mvc;
using server.Exceptions;
using server.Models.Common;

namespace server.Extensions;

/// <summary>
/// Extension methods for controllers to easily return standardized error responses
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    public static ObjectResult CreateErrorResponse(
        this ControllerBase controller,
        int statusCode,
        ErrorTypes errorType,
        string message,
        string? detail = null,
        Dictionary<string, object>? metadata = null)
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            Type = errorType.ToStringValue(),
            Message = message,
            Detail = detail,
            TraceId = controller.HttpContext.TraceIdentifier,
            Metadata = metadata
        };

        return new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Creates a validation error response
    /// </summary>
    public static BadRequestObjectResult CreateValidationErrorResponse(
        this ControllerBase controller,
        Dictionary<string, List<string>> validationErrors,
        string message = "Validation failed")
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = 400,
            Type = ErrorTypes.ValidationError.ToStringValue(),
            Message = message,
            ValidationErrors = validationErrors,
            TraceId = controller.HttpContext.TraceIdentifier
        };

        return new BadRequestObjectResult(errorResponse);
    }

    /// <summary>
    /// Creates a not found error response
    /// </summary>
    public static NotFoundObjectResult CreateNotFoundResponse(
        this ControllerBase controller,
        string resourceType,
        object id)
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = 404,
            Type = ErrorTypes.NotFound.ToStringValue(),
            Message = $"{resourceType} with ID '{id}' was not found.",
            TraceId = controller.HttpContext.TraceIdentifier
        };

        return new NotFoundObjectResult(errorResponse);
    }
}
