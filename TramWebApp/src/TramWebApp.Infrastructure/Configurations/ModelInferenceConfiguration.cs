namespace TramWebApp.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TramWebApp.Domain.Entities;

public class ModelInferenceConfiguration : IEntityTypeConfiguration<ModelInference>
{
    public void Configure(EntityTypeBuilder<ModelInference> builder)
    {
        builder.ToTable("model_inferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.TramRunId).HasColumnName("tram_run_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.InferenceTime).HasColumnName("inference_time").IsRequired();
        builder.Property(x => x.InputControllerPosition).HasColumnName("input_controller_position").IsRequired();
        builder.Property(x => x.InputFrontSpeed).HasColumnName("input_front_speed").IsRequired();
        builder.Property(x => x.InputRearSpeed).HasColumnName("input_rear_speed").IsRequired();
        builder.Property(x => x.PredictedSpeed).HasColumnName("predicted_speed").IsRequired();
        builder.Property(x => x.PredictedPosition).HasColumnName("predicted_position").IsRequired();
        builder.Property(x => x.ActualSpeed).HasColumnName("actual_speed");
        builder.Property(x => x.ActualPosition).HasColumnName("actual_position");
        builder.Property(x => x.UserAdjustedSpeed).HasColumnName("user_adjusted_speed");
        builder.Property(x => x.UserAdjustedPosition).HasColumnName("user_adjusted_position");
        builder.Property(x => x.IsUserIntervention).HasColumnName("is_user_intervention").IsRequired().HasDefaultValue(false);
        builder.Property(x => x.ErrorSpeed).HasColumnName("error_speed");
        builder.Property(x => x.ErrorPosition).HasColumnName("error_position");

        builder.HasIndex(x => x.TramRunId).HasDatabaseName("ix_model_inferences_tram_run_id");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_model_inferences_user_id");
        builder.HasIndex(x => x.InferenceTime).HasDatabaseName("ix_model_inferences_inference_time");
    }
}