using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.ToTable("user_skills");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();

        builder.HasIndex(s => new { s.UserId, s.SkillId }).IsUnique();

        builder.Property(s => s.SelfRatedLevel).IsRequired();
        builder.Property(s => s.YearsExperience);
        builder.Property(s => s.IsPrimary).IsRequired();
        builder.Property(s => s.LastUsed);

        builder.HasOne(s => s.Skill)
            .WithMany()
            .HasForeignKey(s => s.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
    }
}
