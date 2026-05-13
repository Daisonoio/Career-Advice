using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class UserAlertConfiguration : IEntityTypeConfiguration<UserAlert>
{
    public void Configure(EntityTypeBuilder<UserAlert> builder)
    {
        builder.ToTable("user_alerts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        builder.HasIndex(a => new { a.UserId, a.ReadAt });

        builder.Property(a => a.AlertType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(a => a.Title).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Body).HasMaxLength(2000);
        builder.Property(a => a.Severity).IsRequired().HasMaxLength(50);
        builder.Property(a => a.ReadAt);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
