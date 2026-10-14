using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence;

internal sealed class SqlServerPartnerRepository(PartnersDbContext db) : IPartnerRepository
{
    // SQL Server error numbers for "Cannot insert duplicate key" (unique index / unique constraint).
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await db.Partners.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return model?.ToDomain();
    }

    public async Task<Partner> AddAsync(Partner partner, CancellationToken cancellationToken)
    {
        var model = partner.ToModel();
        db.Partners.Add(model);
        await SaveAsync(partner, cancellationToken);
        return model.ToDomain();
    }

    public async Task<Partner> UpdateAsync(Partner partner, byte[] expectedVersion, CancellationToken cancellationToken)
    {
        var model = partner.ToModel();
        var entry = db.Partners.Attach(model);
        entry.State = EntityState.Modified;
        entry.Property(p => p.CreatedAt).IsModified = false;

        // UPDATE ... WHERE Id = @id AND Version = @expectedVersion — compared atomically by SQL Server.
        entry.Property(p => p.Version).OriginalValue = expectedVersion;

        await SaveAsync(partner, cancellationToken);
        return model.ToDomain();
    }

    private async Task SaveAsync(Partner partner, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            throw new PartnerConcurrencyException(partner.Id);
        }
        catch (DbUpdateException ex) when (IsTaxNumberUniqueViolation(ex))
        {
            db.ChangeTracker.Clear();
            throw new DuplicateTaxNumberException(partner.TaxNumber.Value);
        }
    }

    private static bool IsTaxNumberUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation } sql
        && sql.Message.Contains(PartnerModelConfiguration.TaxNumberUniqueIndex, StringComparison.Ordinal);
}
