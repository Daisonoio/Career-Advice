using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aegis.Infrastructure.Persistence.Configurations;

public class MarketKpiConfiguration : IEntityTypeConfiguration<MarketKpi>
{
    public void Configure(EntityTypeBuilder<MarketKpi> builder)
    {
        builder.ToTable("market_kpis");

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).UseIdentityColumn();

        builder.HasIndex(k => new { k.SkillId, k.RoleCanonical, k.GeoCountry });

        builder.Property(k => k.RoleCanonical).HasMaxLength(200);
        builder.Property(k => k.GeoCountry).HasMaxLength(10);
        builder.Property(k => k.GeoCity).HasMaxLength(100);
        builder.Property(k => k.SalaryCurrency).HasMaxLength(10);

        builder.Property(k => k.SalaryMedian).HasColumnType("decimal(18,2)");
        builder.Property(k => k.SalaryP25).HasColumnType("decimal(18,2)");
        builder.Property(k => k.SalaryP75).HasColumnType("decimal(18,2)");

        builder.Property(k => k.CreatedAt).IsRequired();
        builder.Property(k => k.UpdatedAt).IsRequired();
    }
}
