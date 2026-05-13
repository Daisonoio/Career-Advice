using Aegis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Aegis.Infrastructure.Persistence;

public class AegisDbContext : DbContext
{
    public AegisDbContext(DbContextOptions<AegisDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillCategory> SkillCategories => Set<SkillCategory>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<MarketKpi> MarketKpis => Set<MarketKpi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AegisDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
