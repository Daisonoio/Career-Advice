using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aegis.Infrastructure.Persistence;

// Used only by EF Core CLI tooling (dotnet ef migrations …).
// Not registered in the DI container.
public class AegisDbContextFactory : IDesignTimeDbContextFactory<AegisDbContext>
{
    public AegisDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AegisDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=aegis_design;Username=aegis;Password=design",
            npg => npg.UseVector());

        return new AegisDbContext(optionsBuilder.Options);
    }
}
