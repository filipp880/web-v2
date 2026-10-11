namespace TramWebApp.Application.DTOs;

public record TramRunDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid CreatedByUserId,
    bool IsActive,
    int TelemetryPointsCount,
    int InferencesCount
);

public record CreateTramRunDto(
    string Name,
    string Description
);

public record UpdateTramRunDto(
    string Name,
    string Description
);

public record TramRunListDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    bool IsActive,
    int TelemetryPointsCount
);