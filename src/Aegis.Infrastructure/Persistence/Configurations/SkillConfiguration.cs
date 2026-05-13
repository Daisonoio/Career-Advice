using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("skills");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.NameIt)
            .HasMaxLength(200);

        builder.Property(s => s.CanonicalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(s => s.CanonicalName).IsUnique();

        builder.Property(s => s.Aliases)
            .HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(s => s.ParentId);

        // pgvector column
        builder.Property(s => s.EmbeddingVector)
            .HasColumnType("vector(1536)");

        // Sync metadata
        builder.Property(s => s.IsSystemSkill)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.SourceType)
            .HasMaxLength(50);

        builder.Property(s => s.SourceExternalId)
            .HasMaxLength(500);

        builder.HasIndex(s => s.SourceExternalId)
            .IsUnique()
            .HasFilter("source_external_id IS NOT NULL");

        builder.Property(s => s.ConfidenceScore)
            .HasDefaultValue(1.0);

        builder.Property(s => s.LastSyncedAt);

        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
