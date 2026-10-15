namespace TramWebApp.Application.DTOs;
using System.ComponentModel.DataAnnotations;
public record SimulationStartDto([Range(0, 80)] double InitialSpeedKmh = 36, bool Winter = true, [Range(-1, 1)] double InitialHandle = 0);
public record SimulationStepDto([Range(-1, 1)] double Handle, [Range(1, 100000)] int SequenceNumber);
public record SimulationCreatedDto(Guid RunId);
public record SimulationStateDto(Guid PointId, int SequenceNumber, double Speed, double Distance, double Timestamp);
