namespace TramWebApp.Infrastructure.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.Enums;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(x => x.Role).HasColumnName("role").IsRequired().HasConversion<int>().HasDefaultValue(UserRole.User);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("ix_users_username");
        builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_users_email");
        builder.HasIndex(x => x.Role).HasDatabaseName("ix_users_role");

        builder.HasMany(x => x.CreatedRuns)
            .WithOne()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Inferences)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
