using AirportTool.Application;
using System.Text.Json;

namespace AirportTool.WebApi;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsRequestCancellation(ex, context))
        {
            _logger.LogInformation(ex, "Request was cancelled by the client.");
        }
        catch (DatabaseConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Database concurrency conflict.");

            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status409Conflict,
                title: "Concurrency conflict",
                detail: ex.Message);
        }
        catch (DatabaseException ex)
        {
            var (status, title, detail, logLevel) = MapDatabaseException(ex);

            _logger.Log(logLevel, ex, "Database error mapped to {Status}: {Title}", status, title);

            await WriteProblemDetailsAsync(
                context,
                statusCode: status,
                title: title,
                detail: detail);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation.");

            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");

            await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred.");
        }
    }

    private static (int Status, string Title, string? Detail, LogLevel LogLevel) MapDatabaseException(DatabaseException ex)
    {
        return ex switch
        {
            DatabaseUniqueConstraintException =>
                (StatusCodes.Status409Conflict, "Duplicate resource", ex.Message, LogLevel.Information),

            DatabaseConstraintException =>
                (StatusCodes.Status409Conflict, "Constraint violation", ex.Message, LogLevel.Information),

            DatabaseNotNullException =>
                (StatusCodes.Status400BadRequest, "Invalid request data", ex.Message, LogLevel.Information),

            DatabaseDataTooLongException =>
                (StatusCodes.Status400BadRequest, "Invalid request data", ex.Message, LogLevel.Information),

            DatabaseDeadlockException =>
                (StatusCodes.Status503ServiceUnavailable, "Database busy", "Please retry the request.", LogLevel.Warning),

            DatabaseTimeoutException =>
                (StatusCodes.Status503ServiceUnavailable, "Database timeout", "Please retry the request.", LogLevel.Warning),

            DatabaseUnavailableException =>
                (StatusCodes.Status503ServiceUnavailable, "Database unavailable", "Please retry later.", LogLevel.Error),

            DatabaseWriteException =>
                (StatusCodes.Status500InternalServerError, "Database error", "An unexpected error occurred.", LogLevel.Error),

            _ =>
                (StatusCodes.Status500InternalServerError, "Database error", "An unexpected error occurred.", LogLevel.Error)
        };
    }

    private static bool IsRequestCancellation(Exception ex, HttpContext context)
    {
        return context.RequestAborted.IsCancellationRequested && ex is OperationCanceledException;
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string? detail = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = new
        {
            type = "about:blank",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}