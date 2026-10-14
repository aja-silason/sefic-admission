namespace Partners.Domain.Partners;

/// <summary>Write-side persistence of <see cref="Partner"/>.</summary>
public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Inserts the partner and returns the persisted state (with its store-assigned Version).</summary>
    /// <exception cref="DuplicateTaxNumberException">The tax number is already registered.</exception>
    Task<Partner> AddAsync(Partner partner, CancellationToken cancellationToken);

    /// <summary>
    /// Persists the partner only if the stored version still equals <paramref name="expectedVersion"/>;
    /// the comparison happens atomically in the database. Returns the persisted state with the new Version.
    /// </summary>
    /// <exception cref="PartnerConcurrencyException">The stored version differs.</exception>
    /// <exception cref="DuplicateTaxNumberException">The tax number belongs to another partner.</exception>
    Task<Partner> UpdateAsync(Partner partner, byte[] expectedVersion, CancellationToken cancellationToken);
}
