using Serilog;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Infrastructure.InMemory;
using TramWebApp.Api.Auth;
using TramWebApp.Api.Middleware;
using TramWebApp.Api.Swagger;
using TramWebApp.Application;
using TramWebApp.Infrastructure;
using TramWebApp.Infrastructure.Identity;
using TramWebApp.Infrastructure.ModelIntegration;
using TramWebApp.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers(options => options.Filters.Add<RequestValidationFilter>())
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
        });

    builder.Services.AddCustomSwagger();
    builder.Services.AddJwtAuthentication(builder.Configuration);

    var storageMode = builder.Configuration.GetValue<TramWebApp.Domain.Enums.StorageMode>("StorageMode", TramWebApp.Domain.Enums.StorageMode.PostgreSQL);

    if (storageMode == TramWebApp.Domain.Enums.StorageMode.PostgreSQL)
    {
        builder.Services.AddDbContext<TramDbContext>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });
        });

        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<ISimulationRepository, TramWebApp.Infrastructure.Repositories.SimulationRepository>();
    }
    else
    {
        builder.Services.AddSingleton<IUnitOfWork>(sp => new InMemoryUnitOfWork(50));
        builder.Services.AddScoped<ISimulationRepository, InMemorySimulationRepository>();
    }

    builder.Services.AddAutoMapper(typeof(TramWebApp.Application.Mappings.MappingProfile).Assembly);
    builder.Services.AddValidatorsFromAssembly(typeof(TramWebApp.Application.Validators.CreateTramRunDtoValidator).Assembly);
    builder.Services.AddScoped<ITramRunRepository>(sp => sp.GetRequiredService<IUnitOfWork>().TramRuns);
    builder.Services.AddScoped<ITramTelemetryRepository>(sp => sp.GetRequiredService<IUnitOfWork>().TelemetryPoints);
    builder.Services.AddScoped<IModelInferenceRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Inferences);
    builder.Services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IUnitOfWork>().Users);

    builder.Services.AddScoped<ITramRunUseCases, TramWebApp.Application.UseCases.TramRunUseCases>();
    builder.Services.AddScoped<ITelemetryUseCases, TramWebApp.Application.UseCases.TelemetryUseCases>();
    builder.Services.AddScoped<IModelUseCases, TramWebApp.Application.UseCases.ModelUseCases>();
    builder.Services.AddScoped<IAuthUseCases, TramWebApp.Application.UseCases.AuthUseCases>();
    builder.Services.AddScoped<ISimulationUseCases, TramWebApp.Application.UseCases.SimulationUseCases>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();
    builder.Services.AddScoped<IRunAccess, TramWebApp.Application.UseCases.RunAccess>();

    builder.Services.AddSingleton<IModelService, TramModelService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost", "http://localhost:5173", "http://localhost:3000"])
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    var healthChecks = builder.Services.AddHealthChecks();
    if (storageMode == TramWebApp.Domain.Enums.StorageMode.PostgreSQL)
        healthChecks.AddCheck("postgresql", () =>
        {
            using var connection = new Npgsql.NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
            connection.Open();
            return HealthCheckResult.Healthy();
        });

    var app = builder.Build();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TramWebApp API v1");
            options.RoutePrefix = "swagger";
            options.DisplayRequestDuration();
        });
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    using (var scope = app.Services.CreateScope())
    {
        if (storageMode == TramWebApp.Domain.Enums.StorageMode.PostgreSQL)
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TramDbContext>();
            await dbContext.Database.MigrateAsync();
            
            await SeedData.InitializeAsync(scope.ServiceProvider);
        }
        else
        {
            await SeedData.InitializeInMemoryAsync(scope.ServiceProvider);
        }
    }

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
