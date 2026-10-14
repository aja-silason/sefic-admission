using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Partners.Domain.Partners;
using Partners.Infrastructure.Persistence;

namespace Partners.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>Optionally applies pending migrations and inserts sample partners into an empty table.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool migrate, bool seedSampleData)
    {
        if (!migrate && !seedSampleData)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PartnersDbContext>();

        if (migrate)
        {
            await db.Database.MigrateAsync();
        }

        if (seedSampleData && !await db.Partners.AnyAsync())
        {
            var now = DateTime.UtcNow;
            db.Partners.AddRange(SampleData.Select(s =>
                Partner.Create(PartnerDetails.Create(s.Name, s.TaxNumber, s.Type, s.Email, s.CreditLimit), now).ToModel()));
            await db.SaveChangesAsync();
        }
    }

    private static readonly (string Name, string TaxNumber, string Type, string? Email, decimal CreditLimit)[] SampleData =
    [
        ("Ana Silva", "001.234.567", PartnerTypes.CustomerCode, "ana.silva@example.com", 5_000m),
        ("Bruno Costa Lda", "502 345 678", PartnerTypes.SupplierCode, "compras@brunocosta.example", 0m),
        ("Carla Mendes", "203-456-789", PartnerTypes.BothCode, null, 12_500.50m),
        ("Distribuidora Atlântico", "504111222", PartnerTypes.SupplierCode, "geral@atlantico.example", 250_000m),
        ("Eduardo Santos", "198765432", PartnerTypes.CustomerCode, "eduardo@example.com", 750.25m),
        ("Mariana Tavares", "245678901", PartnerTypes.BothCode, "mariana.t@example.com", 1_000_000m)
    ];
}
