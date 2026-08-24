using System.Net;
using System.Text.Json;
using FluentValidation;
using GramShopPOS.Application.Common;
using GramShopPOS.Application.Exceptions;

namespace GramShopPOS.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
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
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        var (status, message, errors) = exception switch
        {
            AppException app => (app.StatusCode, app.Message, app.Errors),
            ValidationException fv => (400, "Validation failed.", fv.Errors.Select(e => e.ErrorMessage).ToList()),
            UnauthorizedAccessException => (401, "Unauthorized.", Array.Empty<string>()),
            _ => (500, "An unexpected error occurred.", Array.Empty<string>())
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode}", status);
        }

        if (_environment.IsDevelopment() && status >= 500)
        {
            message = exception.Message;
            errors = [exception.ToString()];
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;
        var payload = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-XSS-Protection"] = "0";
        context.Response.Headers["Cache-Control"] = "no-store";
        return _next(context);
    }
}
