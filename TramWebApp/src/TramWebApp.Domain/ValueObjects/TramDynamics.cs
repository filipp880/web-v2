namespace TramWebApp.Domain.ValueObjects;

// Illustrative training dynamics, not a calibrated vehicle or safety model.
public static class TramDynamics
{
    public static (double Speed, double Distance) Advance(double speed, double handle, bool winter, double seconds)
    {
        var velocity = Math.Max(0, speed);
        double distance = 0;
        for (double time = 0; time < seconds;)
        {
            var dt = Math.Min(0.05, seconds - time);
            var acceleration = handle >= 0 ? handle * (winter ? 0.35 : 0.8) : handle * (winter ? 0.28 : 0.65);
            var next = Math.Clamp(velocity + (acceleration - 0.02 - 0.001 * velocity) * dt, 0, 22.22);
            distance += (velocity + next) / 2 * dt;
            velocity = next; time += dt;
        }
        return (velocity, distance);
    }
}
