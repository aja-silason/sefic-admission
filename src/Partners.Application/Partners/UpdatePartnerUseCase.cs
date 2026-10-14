using Partners.Domain.Common;
using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed class UpdatePartnerUseCase(IPartnerRepository repository, TimeProvider clock)
{
    public async Task<PartnerResponse> ExecuteAsync(Guid id, UpdatePartnerInput input, CancellationToken cancellationToken)
    {
        // Everything is validated before touching the database, so an invalid request changes nothing.
        var errors = new ValidationErrors();
        PartnerDetails? details = null;
        try
        {
            details = PartnerDetails.Create(input.Name, input.TaxNumber, input.Type, input.Email, input.CreditLimit);
        }
        catch (DomainValidationException ex)
        {
            errors.AddRange(ex);
        }

        if (input.IsActive is null)
        {
            errors.Add(nameof(input.IsActive), "IsActive is required.");
        }

        if (input.Version is null)
        {
            errors.Add(nameof(input.Version), "Version is required.");
        }

        byte[]? expectedVersion = null;
        if (input.Version is not null && !VersionToken.TryDecode(input.Version, out expectedVersion))
        {
            errors.Add(nameof(input.Version), "Version is malformed; use the value returned by the API.");
        }

        errors.ThrowIfAny();

        var partner = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new PartnerNotFoundException(id);

        partner.Update(details!, input.IsActive!.Value, clock.GetUtcNow().UtcDateTime);

        // The version check is part of the UPDATE statement itself (optimistic concurrency in SQL Server).
        var saved = await repository.UpdateAsync(partner, expectedVersion!, cancellationToken);
        return PartnerResponse.From(saved);
    }
}
