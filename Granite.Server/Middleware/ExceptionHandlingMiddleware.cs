using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Granite.Common.Dto.JsonApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Granite.Server.Middleware;

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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var errorCode = GetErrorCode(exception);

        var errorDocument = new JsonApiDocument<object>
        {
            Data = null,
            Errors = new List<JsonApiError>
            {
                new JsonApiError
                {
                    Code = errorCode,
                    Message = exception.Message,
                    StackTrace = context.Request.Host.Host == "localhost" || context.Request.Host.Host == "127.0.0.1"
                        ? exception.StackTrace
                        : null
                }
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorDocument, options));
    }

    private static HttpStatusCode GetStatusCode(Exception exception) =>
        exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            KeyNotFoundException => HttpStatusCode.NotFound,
            NotImplementedException => HttpStatusCode.NotImplemented,
            _ => HttpStatusCode.InternalServerError
        };

    private static string GetErrorCode(Exception exception) =>
        exception switch
        {
            ArgumentNullException => "INVALID_ARGUMENT",
            ArgumentException => "INVALID_ARGUMENT",
            InvalidOperationException => "INVALID_OPERATION",
            UnauthorizedAccessException => "UNAUTHORIZED",
            KeyNotFoundException => "NOT_FOUND",
            NotImplementedException => "NOT_IMPLEMENTED",
            _ => "INTERNAL_ERROR"
        };
}
