using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class RecommendationPathConfiguration : IEntityTypeConfiguration<RecommendationPath>
{
    public void Configure(EntityTypeBuilder<RecommendationPath> builder)
    {
        builder.ToTable("recommendation_paths");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).UseIdentityColumn();

        builder.Property(p => p.TargetRole).IsRequired().HasMaxLength(200);
        builder.Property(p => p.TargetRoleCanonical).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Rank).IsRequired();
        builder.Property(p => p.TransitionDifficulty).IsRequired().HasMaxLength(50);
        builder.Property(p => p.LlmRationale).HasMaxLength(2000);

        builder.Property(p => p.SkillGaps)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasOne(p => p.Recommendation)
            .WithMany(r => r.Paths)
            .HasForeignKey(p => p.RecommendationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
