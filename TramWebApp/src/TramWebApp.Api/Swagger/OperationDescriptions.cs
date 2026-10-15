namespace TramWebApp.Api.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class OperationDescriptions : IOperationFilter
{
    private static readonly Dictionary<string, string> Summaries = new()
    {
        ["Auth.Register"] = "Регистрация пользователя", ["Auth.Login"] = "Вход и получение JWT",
        ["Auth.GetProfile"] = "Профиль текущего пользователя", ["Auth.Me"] = "Профиль текущего пользователя",
        ["TramRuns.GetAll"] = "Список прогонов", ["TramRuns.GetById"] = "Данные выбранного прогона",
        ["TramRuns.Create"] = "Создать прогон", ["TramRuns.Update"] = "Изменить название и описание прогона",
        ["TramRuns.Delete"] = "Удалить прогон и связанные данные",
        ["Telemetry.GetByRunId"] = "Телеметрия прогона по порядку шагов", ["Telemetry.GetBySequence"] = "Одна точка телеметрии",
        ["Telemetry.Create"] = "Записать точку телеметрии", ["Telemetry.CreateBatch"] = "Записать набор точек телеметрии",
        ["Model.Predict"] = "Рассчитать скорость и положение", ["Model.PredictBatch"] = "Расчет последовательности с накоплением пути",
        ["Model.CalculateMetrics"] = "Рассчитать ошибки относительно эталона", ["Model.RunInference"] = "Рассчитать и сохранить результат в истории",
        ["Model.GetInferencesByRun"] = "История результатов прогона", ["Model.GetMyInferences"] = "Мои сохраненные результаты",
        ["Model.GetInference"] = "Подробности сохраненного результата", ["Model.UserIntervention"] = "Сохранить корректировку оператора",
        ["Model.SetActualValues"] = "Указать эталонные значения для результата", ["Model.GetModelStatus"] = "Состояние и версия демонстрационной модели",
        ["Simulation.Start"] = "Создать отдельный прогон тренажера", ["Simulation.Step"] = "Рассчитать и атомарно записать секунду движения",
        ["System.Storage"] = "Режим хранения и лимит прогонов"
    };
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var key = context.ApiDescription.ActionDescriptor.RouteValues["controller"] + "." + context.MethodInfo.Name;
        operation.Summary = Summaries.GetValueOrDefault(key, context.MethodInfo.Name);
        var anonymous = context.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), true) || context.MethodInfo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true ||
            (!context.MethodInfo.IsDefined(typeof(AuthorizeAttribute), true) && context.MethodInfo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), true) != true);
        var admin = context.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType<AuthorizeAttribute>().Any(attribute => attribute.Roles?.Contains("Admin") == true);
        operation.Description = anonymous ? "Открытый метод. JWT не требуется." : admin ? "Доступен только администратору. Требуется JWT." : "Требуется JWT. Доступ к прогону и его данным: владелец или администратор.";
        if (key == "Simulation.Step") operation.Description += " SequenceNumber должен быть следующим номером; повтор номера не создает дубликат. Время шага — 1 секунда. Speed в м/с, Distance в метрах. Учебная динамика.";
        if (key == "TramRuns.Delete") operation.Description += " Телеметрия и результаты удаляются вместе с прогоном.";
        if (key == "Model.Predict" || key == "Model.PredictBatch") operation.Description += " Демонстрационная модель 1.0.0-mock, не обученная модель из ml_model.yaml.";
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Некорректные входные значения" });
        if (anonymous) operation.Security = [];
        else
        {
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            }];
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "JWT отсутствует или недействителен" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Нет доступа к чужому прогону или недостаточно прав" });
        }
        operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Дубликат, нарушение последовательности или лимит 50 прогонов в памяти" });
    }
}
