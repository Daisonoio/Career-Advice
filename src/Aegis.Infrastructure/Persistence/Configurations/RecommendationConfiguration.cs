using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("recommendations");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.HasIndex(r => r.UserId);

        builder.Property(r => r.GeneratedAt).IsRequired();
        builder.Property(r => r.MarketFitScore).IsRequired();
        builder.Property(r => r.FutureRiskScore).IsRequired();
        builder.Property(r => r.CompetitiveScore).IsRequired();
        builder.Property(r => r.SalaryPercentile).IsRequired();
        builder.Property(r => r.LlmExecutiveSummary).HasMaxLength(5000);
        builder.Property(r => r.GenerationConfidence).IsRequired();

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
