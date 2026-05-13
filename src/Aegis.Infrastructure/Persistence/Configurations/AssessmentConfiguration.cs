using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.EstimatedSeniority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.Confidence);
        builder.Property(a => a.CurrentLayer).IsRequired();
        builder.Property(a => a.CompletedAt);

        builder.HasOne<User>()
            .WithMany(u => u.Assessments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
