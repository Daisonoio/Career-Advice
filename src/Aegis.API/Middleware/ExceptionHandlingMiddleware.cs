using Aegis.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ValidationException = Aegis.Application.Common.Exceptions.ValidationException;

namespace Aegis.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, errors) = exception switch
        {
            NotFoundException nfe => (StatusCodes.Status404NotFound, "Not Found", (IDictionary<string, string[]>?)null),
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation Error", ve.Errors),
            UnauthorizedException ue => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            ConflictException ce => (StatusCodes.Status409Conflict, "Conflict", null),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", null)
        };

        context.Response.StatusCode = statusCode;

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else if (statusCode == 400)
        {
            _logger.LogWarning(exception, "Validation error: {Message}", exception.Message);
        }

        object problemDetails;

        if (errors is not null)
        {
            // Validation error with field details
            problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = title,
                status = statusCode,
                errors = errors,
                traceId = context.TraceIdentifier
            };
        }
        else
        {
            var detail = _env.IsProduction() && statusCode >= 500
                ? "An unexpected error occurred."
                : exception.Message;

            problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = context.Request.Path
            };

            ((ProblemDetails)problemDetails).Extensions["traceId"] = context.TraceIdentifier;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
}
