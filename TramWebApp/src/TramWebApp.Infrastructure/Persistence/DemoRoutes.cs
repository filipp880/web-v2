namespace TramWebApp.Infrastructure.Persistence;

using System.Text.Json;

public static class DemoRoutes
{
    private static readonly double[][] Center = Load();
    private static double[][] Load()
    {
        using var stream = typeof(DemoRoutes).Assembly.GetManifestResourceStream("TramWebApp.Infrastructure.Persistence.Data.moscow-tram.json")!;
        return JsonSerializer.Deserialize<double[][]>(stream)!;
    }
    public static double[][] ForName(string name) => name.Contains("Кольцевая")
        ? Center.Concat(Center.Reverse().Skip(1)).ToArray()
        : name.Contains("Радиальный") ? Center.Reverse().ToArray() : Center;

    public static double Length(double[] a, double[] b) => Math.Sqrt(Math.Pow((a[0] - b[0]) * 111195, 2)
        + Math.Pow((a[1] - b[1]) * 111195 * Math.Cos(a[0] * Math.PI / 180), 2));

    public static double[] AtDistance(double[][] route, double distance)
    {
        for (var index = 1; index < route.Length; index++)
        {
            var length = Length(route[index - 1], route[index]);
            if (distance <= length && length > 0)
            {
                var fraction = Math.Clamp(distance / length, 0, 1);
                return [route[index - 1][0] + fraction * (route[index][0] - route[index - 1][0]),
                    route[index - 1][1] + fraction * (route[index][1] - route[index - 1][1])];
            }
            distance -= length;
        }
        return route[^1];
    }

    public static void Apply(TramWebApp.Domain.Entities.TramRun run, IEnumerable<TramWebApp.Domain.Entities.TramTelemetryPoint> telemetry)
    {
        var route = ForName(run.Name);
        run.SetPlannedRoute(route);
        var points = telemetry.OrderBy(point => point.SequenceNumber).ToArray();
        double distance = 0;
        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            if (index > 0)
            {
                var previous = points[index - 1];
                distance += (previous.FrontBogieSpeed + previous.RearBogieSpeed + point.FrontBogieSpeed + point.RearBogieSpeed) / 4
                    * Math.Max(0, point.Timestamp - previous.Timestamp);
            }
            var coordinate = AtDistance(route, distance);
            var hasGps = index < 5 || index % 10 == 0;
            point.UpdateGps(hasGps ? coordinate[0] : null, hasGps ? coordinate[1] : null,
                hasGps ? Math.Max(0, point.GpsSpeed ?? (point.FrontBogieSpeed + point.RearBogieSpeed) / 2) : null);
        }
    }
}
