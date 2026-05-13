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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<RecommendationPath> RecommendationPaths => Set<RecommendationPath>();
    public DbSet<UserAlert> UserAlerts => Set<UserAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AegisDbContext).Assembly);

        SeedSkillCategories(modelBuilder);
        SeedSkills(modelBuilder);
        SeedMarketKpis(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void SeedSkillCategories(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<SkillCategory>().HasData(
            new { Id = 1, Name = "Languages", Description = "Programming languages", CreatedAt = now, UpdatedAt = now },
            new { Id = 2, Name = "Frameworks", Description = "Frameworks and libraries", CreatedAt = now, UpdatedAt = now },
            new { Id = 3, Name = "Cloud", Description = "Cloud platforms and services", CreatedAt = now, UpdatedAt = now },
            new { Id = 4, Name = "DevOps", Description = "DevOps tools and practices", CreatedAt = now, UpdatedAt = now },
            new { Id = 5, Name = "Architecture", Description = "Software architecture patterns", CreatedAt = now, UpdatedAt = now },
            new { Id = 6, Name = "Data", Description = "Data storage and processing", CreatedAt = now, UpdatedAt = now },
            new { Id = 7, Name = "AI/ML", Description = "Artificial intelligence and machine learning", CreatedAt = now, UpdatedAt = now },
            new { Id = 8, Name = "Leadership", Description = "Technical leadership and soft skills", CreatedAt = now, UpdatedAt = now }
        );
    }

    private static void SeedSkills(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var emptyAliases = "[]";

        modelBuilder.Entity<Skill>().HasData(
            // Languages (CategoryId = 1)
            new { Id = 1, Name = "C#", CanonicalName = "csharp", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 2, Name = "Java", CanonicalName = "java", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 3, Name = "Python", CanonicalName = "python", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 4, Name = "TypeScript", CanonicalName = "typescript", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 5, Name = "Go", CanonicalName = "go", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 6, Name = "Rust", CanonicalName = "rust", CategoryId = (int?)1, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Frameworks (CategoryId = 2)
            new { Id = 7, Name = ".NET/ASP.NET Core", CanonicalName = "dotnet-aspnet-core", CategoryId = (int?)2, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 8, Name = "Spring Boot", CanonicalName = "spring-boot", CategoryId = (int?)2, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 9, Name = "FastAPI", CanonicalName = "fastapi", CategoryId = (int?)2, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 10, Name = "React", CanonicalName = "react", CategoryId = (int?)2, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 11, Name = "Node.js", CanonicalName = "nodejs", CategoryId = (int?)2, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Cloud (CategoryId = 3)
            new { Id = 12, Name = "AWS", CanonicalName = "aws", CategoryId = (int?)3, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 13, Name = "Azure", CanonicalName = "azure", CategoryId = (int?)3, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 14, Name = "GCP", CanonicalName = "gcp", CategoryId = (int?)3, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 15, Name = "Kubernetes", CanonicalName = "kubernetes", CategoryId = (int?)3, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 16, Name = "Docker", CanonicalName = "docker", CategoryId = (int?)3, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // DevOps (CategoryId = 4)
            new { Id = 17, Name = "CI/CD", CanonicalName = "cicd", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 18, Name = "Terraform", CanonicalName = "terraform", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 19, Name = "Ansible", CanonicalName = "ansible", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 20, Name = "Prometheus/Grafana", CanonicalName = "prometheus-grafana", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 21, Name = "ELK Stack", CanonicalName = "elk-stack", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Architecture (CategoryId = 5)
            new { Id = 22, Name = "Microservices", CanonicalName = "microservices", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 23, Name = "Event-Driven Architecture", CanonicalName = "event-driven", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 24, Name = "CQRS", CanonicalName = "cqrs", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 25, Name = "Domain-Driven Design", CanonicalName = "ddd", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 26, Name = "REST APIs", CanonicalName = "rest-apis", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 27, Name = "gRPC", CanonicalName = "grpc", CategoryId = (int?)5, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Data (CategoryId = 6)
            new { Id = 28, Name = "PostgreSQL", CanonicalName = "postgresql", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 29, Name = "MongoDB", CanonicalName = "mongodb", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 30, Name = "Redis", CanonicalName = "redis", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 31, Name = "Elasticsearch", CanonicalName = "elasticsearch", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 32, Name = "Apache Kafka", CanonicalName = "apache-kafka", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // AI/ML (CategoryId = 7)
            new { Id = 33, Name = "Machine Learning", CanonicalName = "machine-learning", CategoryId = (int?)7, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 34, Name = "LLM Integration", CanonicalName = "llm-integration", CategoryId = (int?)7, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 35, Name = "MLOps", CanonicalName = "mlops", CategoryId = (int?)7, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 36, Name = "TensorFlow/PyTorch", CanonicalName = "tensorflow-pytorch", CategoryId = (int?)7, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Leadership (CategoryId = 8)
            new { Id = 37, Name = "Team Leadership", CanonicalName = "team-leadership", CategoryId = (int?)8, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 38, Name = "System Design", CanonicalName = "system-design", CategoryId = (int?)8, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 39, Name = "Code Review", CanonicalName = "code-review", CategoryId = (int?)8, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 40, Name = "Mentoring", CanonicalName = "mentoring", CategoryId = (int?)8, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },

            // Additional skills — common in .NET ecosystem and roadmap phases
            new { Id = 41, Name = "SQL Server", CanonicalName = "sql-server", CategoryId = (int?)6, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now },
            new { Id = 42, Name = "OpenTelemetry", CanonicalName = "opentelemetry", CategoryId = (int?)4, ParentId = (int?)null, Aliases = emptyAliases, EmbeddingVector = (float[]?)null, CreatedAt = now, UpdatedAt = now }
        );
    }

    private static void SeedMarketKpis(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // European IT market 2024 data (EUR)
        modelBuilder.Entity<MarketKpi>().HasData(
            // Backend Engineer
            new
            {
                Id = 1,
                SkillId = (int?)null,
                RoleCanonical = "Backend Engineer",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)8.2,
                JobCount30d = (int?)4800,
                JobCountYoyGrowth = (double?)0.12,
                SalaryMedian = (decimal?)72000m,
                SalaryP25 = (decimal?)58000m,
                SalaryP75 = (decimal?)90000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.15,
                SaturationIndex = (double?)4.5,
                EstimatedApplicantsPerJob = (double?)12.0,
                AIRiskScore = (double?)3.5,
                AutomationProbability = (double?)0.28,
                GrowthMomentum = (double?)7.1,
                SkillMentionTrend90d = (double?)0.08,
                CareerStability = (double?)7.8,
                LayoffSensitivity = (double?)0.18,
                DataConfidence = (double?)0.82,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Platform Engineer
            new
            {
                Id = 2,
                SkillId = (int?)null,
                RoleCanonical = "Platform Engineer",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)7.8,
                JobCount30d = (int?)2200,
                JobCountYoyGrowth = (double?)0.22,
                SalaryMedian = (decimal?)80000m,
                SalaryP25 = (decimal?)65000m,
                SalaryP75 = (decimal?)100000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.20,
                SaturationIndex = (double?)3.2,
                EstimatedApplicantsPerJob = (double?)8.0,
                AIRiskScore = (double?)2.8,
                AutomationProbability = (double?)0.20,
                GrowthMomentum = (double?)8.5,
                SkillMentionTrend90d = (double?)0.18,
                CareerStability = (double?)8.2,
                LayoffSensitivity = (double?)0.12,
                DataConfidence = (double?)0.75,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Software Architect
            new
            {
                Id = 3,
                SkillId = (int?)null,
                RoleCanonical = "Software Architect",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)7.2,
                JobCount30d = (int?)1500,
                JobCountYoyGrowth = (double?)0.08,
                SalaryMedian = (decimal?)95000m,
                SalaryP25 = (decimal?)78000m,
                SalaryP75 = (decimal?)120000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.12,
                SaturationIndex = (double?)3.8,
                EstimatedApplicantsPerJob = (double?)10.0,
                AIRiskScore = (double?)2.2,
                AutomationProbability = (double?)0.15,
                GrowthMomentum = (double?)6.5,
                SkillMentionTrend90d = (double?)0.05,
                CareerStability = (double?)8.8,
                LayoffSensitivity = (double?)0.10,
                DataConfidence = (double?)0.78,
                CreatedAt = now,
                UpdatedAt = now
            },
            // DevOps Engineer
            new
            {
                Id = 4,
                SkillId = (int?)null,
                RoleCanonical = "DevOps Engineer",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)8.5,
                JobCount30d = (int?)3500,
                JobCountYoyGrowth = (double?)0.18,
                SalaryMedian = (decimal?)78000m,
                SalaryP25 = (decimal?)63000m,
                SalaryP75 = (decimal?)98000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.22,
                SaturationIndex = (double?)4.0,
                EstimatedApplicantsPerJob = (double?)10.5,
                AIRiskScore = (double?)2.5,
                AutomationProbability = (double?)0.18,
                GrowthMomentum = (double?)8.0,
                SkillMentionTrend90d = (double?)0.15,
                CareerStability = (double?)8.0,
                LayoffSensitivity = (double?)0.14,
                DataConfidence = (double?)0.80,
                CreatedAt = now,
                UpdatedAt = now
            },
            // AI/ML Engineer
            new
            {
                Id = 5,
                SkillId = (int?)null,
                RoleCanonical = "AI/ML Engineer",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)9.1,
                JobCount30d = (int?)2800,
                JobCountYoyGrowth = (double?)0.45,
                SalaryMedian = (decimal?)98000m,
                SalaryP25 = (decimal?)78000m,
                SalaryP75 = (decimal?)130000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.18,
                SaturationIndex = (double?)2.5,
                EstimatedApplicantsPerJob = (double?)6.0,
                AIRiskScore = (double?)1.5,
                AutomationProbability = (double?)0.10,
                GrowthMomentum = (double?)9.5,
                SkillMentionTrend90d = (double?)0.35,
                CareerStability = (double?)7.5,
                LayoffSensitivity = (double?)0.22,
                DataConfidence = (double?)0.72,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Tech Lead
            new
            {
                Id = 6,
                SkillId = (int?)null,
                RoleCanonical = "Tech Lead",
                GeoCountry = "EU",
                GeoCity = (string?)null,
                DemandScore = (double?)7.5,
                JobCount30d = (int?)1800,
                JobCountYoyGrowth = (double?)0.10,
                SalaryMedian = (decimal?)105000m,
                SalaryP25 = (decimal?)85000m,
                SalaryP75 = (decimal?)130000m,
                SalaryCurrency = "EUR",
                RemotePremiumPct = (double?)0.10,
                SaturationIndex = (double?)3.5,
                EstimatedApplicantsPerJob = (double?)9.0,
                AIRiskScore = (double?)2.0,
                AutomationProbability = (double?)0.12,
                GrowthMomentum = (double?)7.0,
                SkillMentionTrend90d = (double?)0.06,
                CareerStability = (double?)9.0,
                LayoffSensitivity = (double?)0.08,
                DataConfidence = (double?)0.76,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}
