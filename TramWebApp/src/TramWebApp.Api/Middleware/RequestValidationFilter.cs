namespace TramWebApp.Api.Middleware;
using System.Collections;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RequestValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();
        async Task Validate(object? value, string path, int depth = 0)
        {
            if (value == null || value is string || depth > 4) return;
            if (value is IEnumerable items)
            {
                var index = 0;
                foreach (var item in items) await Validate(item, path + "[" + index++ + "]", depth + 1);
                return;
            }
            var type = value.GetType();
            if (type.Namespace?.StartsWith("TramWebApp") != true) return;
            if (services.GetService(typeof(IValidator<>).MakeGenericType(type)) is IValidator validator)
            {
                var result = await validator.ValidateAsync(new ValidationContext<object>(value), context.HttpContext.RequestAborted);
                foreach (var error in result.Errors)
                {
                    var key = path + "." + error.PropertyName;
                    if (!errors.TryGetValue(key, out var messages)) errors[key] = messages = [];
                    messages.Add(error.ErrorMessage);
                }
            }
            foreach (var property in type.GetProperties().Where(property => property.GetIndexParameters().Length == 0))
                await Validate(property.GetValue(value), path + "." + property.Name, depth + 1);
        }
        foreach (var argument in context.ActionArguments) await Validate(argument.Value, argument.Key);
        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors.ToDictionary(pair => pair.Key, pair => pair.Value.Distinct().ToArray())) { Status = 400, Title = "Проверьте входные данные" });
            return;
        }
        await next();
    }
}
