using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Partners.Infrastructure.Persistence;

/// <summary>Used only by the <c>dotnet ef</c> tooling (migrations add / database update).</summary>
internal sealed class PartnersDbContextFactory : IDesignTimeDbContextFactory<PartnersDbContext>
{
    private const string DefaultConnectionString =
        "Server=localhost,14333;Database=Partners;User Id=sa;Password=Partners#Dev2026;TrustServerCertificate=True";

    public PartnersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Partners") ?? DefaultConnectionString;
        var options = new DbContextOptionsBuilder<PartnersDbContext>().UseSqlServer(connectionString).Options;
        return new PartnersDbContext(options);
    }
}
