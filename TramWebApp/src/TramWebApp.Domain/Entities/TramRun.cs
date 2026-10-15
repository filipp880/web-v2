namespace TramWebApp.Domain.Entities;

public class TramRun
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public bool IsActive { get; private set; }
    public string PlannedRouteJson { get; private set; } = "[]";
    public double[][] PlannedRoute => System.Text.Json.JsonSerializer.Deserialize<double[][]>(PlannedRouteJson) ?? [];
    public void SetPlannedRoute(double[][] coordinates) => PlannedRouteJson = System.Text.Json.JsonSerializer.Serialize(coordinates);
    
    public ICollection<TramTelemetryPoint> TelemetryPoints { get; private set; } = new List<TramTelemetryPoint>();
    public ICollection<ModelInference> Inferences { get; private set; } = new List<ModelInference>();

    private TramRun() { }

    public TramRun(string name, string description, Guid createdByUserId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
        IsActive = true;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
