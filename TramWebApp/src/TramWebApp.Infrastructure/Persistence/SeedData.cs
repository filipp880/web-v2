namespace TramWebApp.Infrastructure.Persistence;

using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Enums;
using TramWebApp.Domain.ValueObjects;
using TramWebApp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TramWebApp.Domain.Interfaces;

public static class SeedData
{
    public static async Task InitializeInMemoryAsync(IServiceProvider services)
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        if (await unitOfWork.Users.ExistsByUsernameAsync("admin")) return;
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var admin = new User("admin", "admin@tramwebapp.local", hasher.HashPassword("admin123"), UserRole.Admin);
        var user = new User("operator", "operator@tramwebapp.local", hasher.HashPassword("user123"), UserRole.User);
        await unitOfWork.Users.AddAsync(admin);
        await unitOfWork.Users.AddAsync(user);
        var model = services.GetRequiredService<IModelService>();
        var random = new Random(42);
        foreach (var name in new[] { "Кольцевая линия", "Радиальный маршрут", "Городской центр" })
        {
            var run = new TramRun(name, "Демонстрационный прогон", admin.Id);
            await unitOfWork.TramRuns.AddAsync(run);
            var points = GenerateTelemetryPoints(run.Id, name, random);
            DemoRoutes.Apply(run, points);
            await unitOfWork.TelemetryPoints.AddRangeAsync(points);
            foreach (var inference in GenerateInferences(run.Id, points, model, admin.Id, user.Id, random))
            {
                await unitOfWork.Inferences.AddAsync(inference);
            }
        }
    }

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TramDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await context.Users.AnyAsync())
        {
            // Upgrade only the original demo runs, keeping IDs, users and inference history.
            var demos = await context.TramRuns.Include(run => run.TelemetryPoints)
                .Where(run => run.PlannedRouteJson == "[]" &&
                    (run.Name == "Тестовый прогон 1 - Кольцевая линия" || run.Name == "Тестовый прогон 2 - Радиальный маршрут" || run.Name == "Тестовый прогон 3 - Городской центр"))
                .ToListAsync();
            foreach (var demo in demos) DemoRoutes.Apply(demo, demo.TelemetryPoints);
            await context.SaveChangesAsync();
            return;
        }

        var adminPasswordHash = passwordHasher.HashPassword("admin123");
        var userPasswordHash = passwordHasher.HashPassword("user123");

        var admin = new User("admin", "admin@tramwebapp.local", adminPasswordHash, UserRole.Admin);
        var user = new User("operator", "operator@tramwebapp.local", userPasswordHash, UserRole.User);

        context.Users.AddRange(admin, user);
        await context.SaveChangesAsync();

        var runs = new List<TramRun>
        {
            new TramRun("Тестовый прогон 1 - Кольцевая линия", "Прогон по кольцевой трассе с остановками", admin.Id),
            new TramRun("Тестовый прогон 2 - Радиальный маршрут", "Прогон по радиальному маршруту от депо до конечной", admin.Id),
            new TramRun("Тестовый прогон 3 - Городской центр", "Прогон через городской центр с частыми остановками", admin.Id)
        };

        context.TramRuns.AddRange(runs);
        await context.SaveChangesAsync();

        var random = new Random(42);
        var allTelemetryPoints = new List<TramTelemetryPoint>();

        foreach (var run in runs)
        {
            var points = GenerateTelemetryPoints(run.Id, run.Name, random);
            DemoRoutes.Apply(run, points);
            allTelemetryPoints.AddRange(points);
        }

        context.TelemetryPoints.AddRange(allTelemetryPoints);
        await context.SaveChangesAsync();

        var modelService = scope.ServiceProvider.GetRequiredService<IModelService>();
        var allInferences = new List<ModelInference>();

        foreach (var run in runs)
        {
            var runPoints = allTelemetryPoints.Where(p => p.TramRunId == run.Id).OrderBy(p => p.SequenceNumber).ToList();
            var inferences = GenerateInferences(run.Id, runPoints, modelService, admin.Id, user.Id, random);
            allInferences.AddRange(inferences);
        }

        context.Inferences.AddRange(allInferences);
        await context.SaveChangesAsync();
    }

    private static List<TramTelemetryPoint> GenerateTelemetryPoints(Guid runId, string runName, Random random)
    {
        var points = new List<TramTelemetryPoint>();
        int sequence = 0;
        double timestamp = 0;
        double position = 0;
        double speed = 0;

        int totalPoints = runName.Contains("Кольцевая") ? 300 : runName.Contains("Радиальный") ? 250 : 200;
        double dt = 1.0;

        for (int i = 0; i < totalPoints; i++)
        {
            double controllerPos;
            double frontSpeed, rearSpeed;

            if (i < totalPoints * 0.1)
            {
                controllerPos = Math.Min(1.0, i / (totalPoints * 0.1));
            }
            else if (i > totalPoints * 0.9)
            {
                controllerPos = Math.Max(-1.0, 1.0 - (i - totalPoints * 0.9) / (totalPoints * 0.1) * 2);
            }
            else
            {
                var phase = (i - totalPoints * 0.1) / (totalPoints * 0.8) * Math.PI * 4;
                controllerPos = 0.5 + 0.3 * Math.Sin(phase) + (random.NextDouble() - 0.5) * 0.2;
                controllerPos = Math.Clamp(controllerPos, -1.0, 1.0);
            }

            var targetSpeed = Math.Max(0, controllerPos) * 15.0 + (random.NextDouble() - 0.5) * 1.0;
            speed = Math.Max(0, speed * 0.9 + targetSpeed * 0.1);
            
            frontSpeed = speed * (0.98 + random.NextDouble() * 0.04);
            rearSpeed = speed * (0.98 + random.NextDouble() * 0.04);

            position += speed * dt;

            double? gpsLat = null, gpsLon = null, gpsSpeed = null;
            if (i % 10 == 0 || i < 5)
            {
                gpsLat = 55.75 + position / 111000.0 + (random.NextDouble() - 0.5) * 0.0001;
                gpsLon = 37.62 + position / (111000.0 * Math.Cos(55.75 * Math.PI / 180)) + (random.NextDouble() - 0.5) * 0.0001;
                gpsSpeed = speed + (random.NextDouble() - 0.5) * 0.5;
            }

            points.Add(new TramTelemetryPoint(
                runId, sequence++, timestamp, controllerPos, frontSpeed, rearSpeed, gpsLat, gpsLon, gpsSpeed));

            timestamp += dt;
        }

        return points;
    }

    private static List<ModelInference> GenerateInferences(
        Guid runId, 
        List<TramTelemetryPoint> points, 
        IModelService modelService, 
        Guid adminId, 
        Guid userId,
        Random random)
    {
        var inferences = new List<ModelInference>();

        foreach (var point in points.Where((_, i) => i % 5 == 0))
        {
            var input = new ModelInput(
                point.ControllerHandlePosition,
                point.FrontBogieSpeed,
                point.RearBogieSpeed,
                point.Timestamp,
                point.GpsLatitude,
                point.GpsLongitude,
                point.GpsSpeed);

            var output = modelService.PredictAsync(input).Result;

            var isUserIntervention = random.NextDouble() < 0.15;
            var inference = new ModelInference(
                runId,
                isUserIntervention ? userId : null,
                point.ControllerHandlePosition,
                point.FrontBogieSpeed,
                point.RearBogieSpeed,
                output.PredictedSpeed,
                output.PredictedPosition,
                point.GpsSpeed,
                point.GpsLatitude.HasValue ? positionFromCoords(point.GpsLatitude.Value, point.GpsLongitude ?? 37.62) : null,
                isUserIntervention);

            if (isUserIntervention)
            {
                var adjustedSpeed = output.PredictedSpeed + (random.NextDouble() - 0.5) * 2.0;
                var adjustedPosition = output.PredictedPosition + (random.NextDouble() - 0.5) * 5.0;
                inference.SetUserIntervention(Math.Max(0, adjustedSpeed), Math.Max(0, adjustedPosition));
            }

            if (point.GpsSpeed.HasValue)
            {
                inference.SetActualValues(point.GpsSpeed.Value, positionFromCoords(point.GpsLatitude ?? 55.75, point.GpsLongitude ?? 37.62));
            }

            inferences.Add(inference);
        }

        return inferences;
    }

    private static double positionFromCoords(double lat, double lon)
    {
        return (lat - 55.75) * 111000.0;
    }
}
