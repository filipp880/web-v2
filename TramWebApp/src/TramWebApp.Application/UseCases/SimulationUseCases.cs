namespace TramWebApp.Application.UseCases;
using TramWebApp.Application.DTOs;
using TramWebApp.Application.Interfaces;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Exceptions;
using TramWebApp.Domain.Interfaces;
using TramWebApp.Domain.ValueObjects;
public class SimulationUseCases(ISimulationRepository repository, IRunAccess access, ICurrentUser user) : ISimulationUseCases
{
    private static readonly SemaphoreSlim StepGate = new(1, 1);
    public async Task<SimulationCreatedDto> StartAsync(Guid sourceId, SimulationStartDto request, CancellationToken cancellationToken = default)
    {
        var source = await access.RequireAsync(sourceId, cancellationToken);
        if (source.PlannedRoute.Length < 2) throw new InvalidModelInputException("У исходного прогона нет заданного маршрута.");
        if (!double.IsFinite(request.InitialSpeedKmh) || request.InitialSpeedKmh < 0 || request.InitialSpeedKmh > 80 ||
            !double.IsFinite(request.InitialHandle) || Math.Abs(request.InitialHandle) > 1)
            throw new InvalidModelInputException("Проверьте начальную скорость и положение ручки.");
        var run = new TramRun("Тренажёр · " + DateTime.UtcNow.ToString("dd.MM HH:mm") + " UTC",
            "simulation:v1;" + (request.Winter ? "winter" : "dry") + "; Автоматическая запись движения", user.Id!.Value);
        run.SetPlannedRoute(source.PlannedRoute);
        var speed = request.InitialSpeedKmh / 3.6;
        var point = new TramTelemetryPoint(run.Id, 0, 0, request.InitialHandle, speed, speed);
        point.SetDistance(0);
        await repository.CreateAsync(run, point, new ModelInference(run.Id, user.Id, request.InitialHandle, speed, speed, speed, 0), cancellationToken);
        return new SimulationCreatedDto(run.Id);
    }
    public async Task<SimulationStateDto> StepAsync(Guid id, SimulationStepDto request, CancellationToken cancellationToken = default)
    {
        if (!double.IsFinite(request.Handle) || Math.Abs(request.Handle) > 1 || request.SequenceNumber < 1 || request.SequenceNumber > 100000)
            throw new InvalidModelInputException("Проверьте ручку и номер шага.");
        await StepGate.WaitAsync(cancellationToken);
        try
        {
            var run = await access.RequireAsync(id, cancellationToken);
            if (!run.Description.StartsWith("simulation:v1;")) throw new InvalidModelInputException("Для записи движения создайте прогон тренажёра.");
            var last = await repository.GetLastPointAsync(id, cancellationToken);
            if (last == null || run.PlannedRoute.Length < 2) throw new InvalidModelInputException("Нет начального состояния или маршрута.");
            if (request.SequenceNumber <= last.SequenceNumber)
                return new SimulationStateDto(last.Id, last.SequenceNumber, last.FrontBogieSpeed, last.DistanceMeters ?? 0, last.Timestamp);
            if (request.SequenceNumber != last.SequenceNumber + 1) throw new ConflictException("Обновите состояние тренажёра.");
            var routeLength = run.PlannedRoute.Zip(run.PlannedRoute.Skip(1), (a, b) =>
                Math.Sqrt(Math.Pow((a[0] - b[0]) * 111195, 2) + Math.Pow((a[1] - b[1]) * 111195 * Math.Cos(a[0] * Math.PI / 180), 2))).Sum();
            var (speed, delta) = TramDynamics.Advance(last.FrontBogieSpeed, request.Handle, run.Description.StartsWith("simulation:v1;winter;"), 1);
            var distance = Math.Min(routeLength, (last.DistanceMeters ?? 0) + delta);
            if (distance >= routeLength) speed = 0;
            var point = new TramTelemetryPoint(id, request.SequenceNumber, last.Timestamp + 1, request.Handle, speed, speed);
            point.SetDistance(distance);
            await repository.AppendAsync(point, new ModelInference(id, user.Id, request.Handle, speed, speed, speed, distance), cancellationToken);
            return new SimulationStateDto(point.Id, point.SequenceNumber, speed, distance, point.Timestamp);
        }
        finally { StepGate.Release(); }
    }
}
