using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();

        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.CurrentRole).HasMaxLength(200);
        builder.Property(p => p.YearsExperience);
        builder.Property(p => p.LocationCountry).HasMaxLength(100);
        builder.Property(p => p.LocationCity).HasMaxLength(100);
        builder.Property(p => p.EnglishLevel).HasMaxLength(50);
        builder.Property(p => p.RemotePreference).HasMaxLength(50);
        builder.Property(p => p.CareerGoals).HasMaxLength(2000);
        builder.Property(p => p.ProfileCompleteness).IsRequired();

        builder.OwnsOne(p => p.SalaryExpectation, sa =>
        {
            sa.Property(s => s.Min).HasColumnName("salary_min").HasColumnType("decimal(18,2)");
            sa.Property(s => s.Max).HasColumnName("salary_max").HasColumnType("decimal(18,2)");
            sa.Property(s => s.Currency).HasColumnName("salary_currency").HasMaxLength(10);
        });

        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
