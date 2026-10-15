namespace TramWebApp.Api.Middleware;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text.Json;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Необработанное исключение: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail) = exception switch
        {
            TramWebApp.Domain.Exceptions.EntityNotFoundException => (StatusCodes.Status404NotFound, "Не найдено", exception.Message),
            TramWebApp.Domain.Exceptions.InMemoryStorageLimitException => (StatusCodes.Status409Conflict, "Конфликт", exception.Message),
            TramWebApp.Domain.Exceptions.ConflictException => (StatusCodes.Status409Conflict, "Конфликт", exception.Message),
            Microsoft.EntityFrameworkCore.DbUpdateException dbException when dbException.InnerException is Npgsql.PostgresException { SqlState: "23505" } =>
                (StatusCodes.Status409Conflict, "Конфликт", "Запись с таким уникальным ключом уже существует."),
            TramWebApp.Domain.Exceptions.InvalidModelInputException => (StatusCodes.Status400BadRequest, "Неверный запрос", exception.Message),
            TramWebApp.Domain.Exceptions.ModelInferenceException => (StatusCodes.Status500InternalServerError, "Ошибка модели", exception.Message),
            TramWebApp.Domain.Exceptions.UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Доступ запрещен", exception.Message),
            TramWebApp.Domain.Exceptions.ValidationException validationEx => (StatusCodes.Status400BadRequest, "Ошибка валидации", validationEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера", "Произошла непредвиденная ошибка")
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        if (exception is TramWebApp.Domain.Exceptions.ValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
