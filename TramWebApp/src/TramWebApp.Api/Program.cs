using Serilog;
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

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
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
    }
    else
    {
        builder.Services.AddSingleton<IUnitOfWork>(sp => new InMemoryUnitOfWork(50));
    }

    builder.Services.AddAutoMapper(typeof(TramWebApp.Application.Mappings.MappingProfile).Assembly);
    builder.Services.AddValidatorsFromAssembly(typeof(TramWebApp.Application.Validators.DtoValidators).Assembly);

    builder.Services.AddScoped<ITramRunUseCases, TramWebApp.Application.UseCases.TramRunUseCases>();
    builder.Services.AddScoped<ITelemetryUseCases, TramWebApp.Application.UseCases.TelemetryUseCases>();
    builder.Services.AddScoped<IModelUseCases, TramWebApp.Application.UseCases.ModelUseCases>();
    builder.Services.AddScoped<IAuthUseCases, TramWebApp.Application.UseCases.AuthUseCases>();

    builder.Services.AddSingleton<IModelService, TramModelService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://frontend:80")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "", name: "postgresql");

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
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

    app.UseHttpsRedirection();
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
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с ошибкой");
}
finally
{
    Log.CloseAndFlush();
}