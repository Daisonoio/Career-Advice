using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class AssessmentQuestionConfiguration : IEntityTypeConfiguration<AssessmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssessmentQuestion> builder)
    {
        builder.ToTable("assessment_questions");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).UseIdentityColumn();

        builder.Property(q => q.QuestionText).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.AnswerText).HasMaxLength(5000);
        builder.Property(q => q.SkillId);
        builder.Property(q => q.DifficultyLevel).IsRequired();
        builder.Property(q => q.Layer).IsRequired();
        builder.Property(q => q.EvaluatedScore);
        builder.Property(q => q.AnsweredAt);

        builder.HasOne<Assessment>()
            .WithMany(a => a.Questions)
            .HasForeignKey(q => q.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.CreatedAt).IsRequired();
        builder.Property(q => q.UpdatedAt).IsRequired();
    }
}
