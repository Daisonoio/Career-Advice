using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class AssessmentResultConfiguration : IEntityTypeConfiguration<AssessmentResult>
{
    public void Configure(EntityTypeBuilder<AssessmentResult> builder)
    {
        builder.ToTable("assessment_results");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityColumn();

        builder.HasOne<Assessment>()
            .WithOne(a => a.Result)
            .HasForeignKey<AssessmentResult>(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.SeniorityScore).IsRequired();
        builder.Property(r => r.OverallConfidence).IsRequired();
        builder.Property(r => r.LlmSummary).HasMaxLength(5000);

        builder.Property(r => r.ValidatedSkills)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ValidatedSkill>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<ValidatedSkill>());

        builder.Property(r => r.WeakSignals)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(r => r.SuspectedInflations)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
    }
}
