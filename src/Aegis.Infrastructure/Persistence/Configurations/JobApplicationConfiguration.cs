using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).UseIdentityColumn();

        builder.HasIndex(j => j.UserId);

        builder.Property(j => j.JobTitle).IsRequired().HasMaxLength(300);
        builder.Property(j => j.CompanyName).HasMaxLength(300);
        builder.Property(j => j.RawDescription).IsRequired().HasMaxLength(20000);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(j => j.RequiredSkillsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(j => j.GapSummaryJson)
            .HasColumnType("jsonb");

        builder.Property(j => j.MatchScorePct);
        builder.Property(j => j.AssessmentId);
        builder.Property(j => j.AnalyzedAt);
        builder.Property(j => j.ScoredAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(j => j.CreatedAt).IsRequired();
        builder.Property(j => j.UpdatedAt).IsRequired();
    }
}
