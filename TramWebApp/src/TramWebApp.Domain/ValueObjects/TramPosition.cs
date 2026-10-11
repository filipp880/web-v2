namespace TramWebApp.Domain.ValueObjects;

public readonly record struct TramPosition
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Heading { get; init; }

    public TramPosition(double x, double y, double heading = 0)
    {
        X = x;
        Y = y;
        Heading = heading;
    }

    public double DistanceTo(TramPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static TramPosition Zero => new(0, 0, 0);
}