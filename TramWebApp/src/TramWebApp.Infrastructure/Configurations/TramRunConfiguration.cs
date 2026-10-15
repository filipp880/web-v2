namespace TramWebApp.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TramWebApp.Domain.Entities;

public class TramRunConfiguration : IEntityTypeConfiguration<TramRun>
{
    public void Configure(EntityTypeBuilder<TramRun> builder)
    {
        builder.ToTable("tram_runs");
        builder.Ignore(x => x.PlannedRoute);
        builder.Property(x => x.PlannedRouteJson).HasColumnName("planned_route").HasColumnType("jsonb").HasDefaultValue("[]");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.CreatedByUserId).HasDatabaseName("ix_tram_runs_created_by_user_id");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_tram_runs_is_active");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_tram_runs_users_created_by");

        builder.HasMany(x => x.TelemetryPoints)
            .WithOne(x => x.TramRun)
            .HasForeignKey(x => x.TramRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Inferences)
            .WithOne(x => x.TramRun)
            .HasForeignKey(x => x.TramRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
