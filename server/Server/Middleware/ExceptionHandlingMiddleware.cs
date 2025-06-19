using System.Net;
using System.Text.Json;
using server.Exceptions;
using server.Models.Common;

namespace server.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning standardized error responses
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            AppException appEx => CreateErrorResponse(appEx, context),
            ArgumentException argEx => CreateValidationErrorResponse(argEx, context),
            UnauthorizedAccessException => CreateErrorResponse(
                401, 
                ErrorType.Unauthorized, 
                "Access denied", 
                context),
            NotImplementedException => CreateErrorResponse(
                501, 
                ErrorType.ServiceUnavailable, 
                "This feature is not yet implemented", 
                context),
            TimeoutException => CreateErrorResponse(
                408, 
                ErrorType.ServiceUnavailable, 
                "The request timed out", 
                context),
            _ => CreateGenericErrorResponse(exception, context)
        };

        context.Response.StatusCode = errorResponse.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private ErrorResponse CreateErrorResponse(AppException appException, HttpContext context)
    {
        var response = new ErrorResponse
        {
            StatusCode = appException.StatusCode,
            Type = appException.ErrorType,
            Message = appException.Message,
            TraceId = context.TraceIdentifier,
            Metadata = appException.Metadata
        };

        // Add validation errors if it's a ValidationException
        if (appException is ValidationException validationEx)
        {
            response = response with { ValidationErrors = validationEx.ValidationErrors };
        }

        // Add detailed error information in development
        if (_environment.IsDevelopment())
        {
            response = response with 
            { 
                Detail = appException.ToString()
            };
        }

        return response;
    }

    private static ErrorResponse CreateErrorResponse(
        int statusCode, 
        ErrorType errorType, 
        string message, 
        HttpContext context)
    {
        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Type = errorType,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        return response;
    }

    private ErrorResponse CreateValidationErrorResponse(ArgumentException exception, HttpContext context)
    {
        var validationErrors = new Dictionary<string, List<string>>
        {
            [exception.ParamName ?? "unknown"] = new List<string> { exception.Message }
        };

        var response = new ErrorResponse
        {
            StatusCode = 400,
            Type = ErrorType.ValidationError,
            Message = "Validation failed",
            ValidationErrors = validationErrors,
            TraceId = context.TraceIdentifier
        };

        if (_environment.IsDevelopment())
        {
            response = response with { Detail = exception.ToString() };
        }

        return response;
    }

    private ErrorResponse CreateGenericErrorResponse(Exception exception, HttpContext context)
    {
        var statusCode = 500;
        var message = _environment.IsDevelopment() 
            ? exception.Message 
            : "An unexpected error occurred. Please try again later.";

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Type = ErrorType.InternalServerError,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        if (_environment.IsDevelopment())
        {
            response = response with 
            { 
                Detail = exception.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["exceptionType"] = exception.GetType().Name,
                    ["stackTrace"] = exception.StackTrace ?? "No stack trace available"
                }
            };
        }

        return response;
    }
}
