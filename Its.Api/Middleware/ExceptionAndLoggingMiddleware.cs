using Its.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Its.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, code, message, errors) = exception switch
        {
            ValidationException vex => (
                HttpStatusCode.BadRequest,
                vex.Code,
                vex.Message,
                vex.Errors.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}")).ToArray()),

            FluentValidation.ValidationException fvex => (
                HttpStatusCode.BadRequest,
                "VALIDATION_FAILED",
                "One or more validation errors occurred.",
                fvex.Errors.Select(e => e.ErrorMessage).ToArray()),

            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Code,
                nfe.Message,
                Array.Empty<string>()),

            ConflictException ce => (
                HttpStatusCode.Conflict,
                ce.Code,
                ce.Message,
                Array.Empty<string>()),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "CONCURRENCY_CONFLICT",
                "The asset record was modified by another user. Please reload the asset details and try again.",
                Array.Empty<string>()),

            ForbiddenException fe => (
                HttpStatusCode.Forbidden,
                fe.Code,
                fe.Message,
                Array.Empty<string>()),

            UnauthorizedException ue => (
                HttpStatusCode.Unauthorized,
                ue.Code,
                ue.Message,
                Array.Empty<string>()),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected server error occurred. Please try again later.",
                Array.Empty<string>())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled domain exception ({StatusCode}): {Message}", statusCode, exception.Message);
        }

        response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            code,
            message,
            errors = errors.Length > 0 ? (object)errors : Array.Empty<string>()
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await response.WriteAsync(json);
    }
}

public class CorrelationIdMiddleware
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items[CorrelationHeader] = correlationId.ToString();
        context.Response.Headers[CorrelationHeader] = correlationId.ToString();

        await _next(context);
    }
}
