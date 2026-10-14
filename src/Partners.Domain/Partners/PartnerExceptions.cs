using Partners.Domain.Common;

namespace Partners.Domain.Partners;

public sealed class PartnerNotFoundException(Guid id)
    : DomainException($"Partner '{id}' was not found.")
{
    public Guid PartnerId { get; } = id;
}

public sealed class DuplicateTaxNumberException(string taxNumber)
    : DomainException($"A partner with TaxNumber '{taxNumber}' already exists.")
{
    public string TaxNumber { get; } = taxNumber;
}

public sealed class PartnerConcurrencyException(Guid id)
    : DomainException($"Partner '{id}' was modified by another request. Reload it and retry with the current Version.")
{
    public Guid PartnerId { get; } = id;
}
