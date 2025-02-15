using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (DuplicateEmailException ex)
        {
            _logger.LogWarning("Duplicate email detected: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "Email is already taken");
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning("User not found: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, "User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new { error = message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
