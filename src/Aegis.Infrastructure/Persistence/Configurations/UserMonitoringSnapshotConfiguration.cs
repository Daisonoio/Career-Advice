using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class UserMonitoringSnapshotConfiguration : IEntityTypeConfiguration<UserMonitoringSnapshot>
{
    public void Configure(EntityTypeBuilder<UserMonitoringSnapshot> builder)
    {
        builder.ToTable("user_monitoring_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.HasIndex(s => new { s.UserId, s.SnapshotDate });

        builder.Property(s => s.SnapshotDate).IsRequired();
        builder.Property(s => s.CompetitiveScore);
        builder.Property(s => s.MarketFitScore);
        builder.Property(s => s.SalaryPercentile);
        builder.Property(s => s.FutureRiskScore);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
