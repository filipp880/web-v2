namespace TramWebApp.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TramWebApp.Domain.Entities;

public class TramTelemetryPointConfiguration : IEntityTypeConfiguration<TramTelemetryPoint>
{
    public void Configure(EntityTypeBuilder<TramTelemetryPoint> builder)
    {
        builder.ToTable("tram_telemetry_points");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TramRunId).HasColumnName("tram_run_id").IsRequired();
        builder.Property(x => x.SequenceNumber).HasColumnName("sequence_number").IsRequired();
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").IsRequired();
        builder.Property(x => x.ControllerHandlePosition).HasColumnName("controller_handle_position").IsRequired();
        builder.Property(x => x.FrontBogieSpeed).HasColumnName("front_bogie_speed").IsRequired();
        builder.Property(x => x.RearBogieSpeed).HasColumnName("rear_bogie_speed").IsRequired();
        builder.Property(x => x.GpsLatitude).HasColumnName("gps_latitude");
        builder.Property(x => x.GpsLongitude).HasColumnName("gps_longitude");
        builder.Property(x => x.GpsSpeed).HasColumnName("gps_speed");
        builder.Property(x => x.DistanceMeters).HasColumnName("distance_meters");

        builder.HasIndex(x => new { x.TramRunId, x.SequenceNumber }).IsUnique().HasDatabaseName("ix_tram_telemetry_run_sequence");
        builder.HasIndex(x => x.TramRunId).HasDatabaseName("ix_tram_telemetry_tram_run_id");
    }
}
