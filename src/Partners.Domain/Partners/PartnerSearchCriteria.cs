namespace Partners.Domain.Partners;

/// <summary>
/// Listing criteria, already resolved into what the store must apply (all combined with AND).
/// </summary>
public sealed record PartnerSearchCriteria
{
    private PartnerSearchCriteria() { }

    /// <summary>Case-insensitive partial match on Name; null when there is no search.</summary>
    public string? NameContains { get; private init; }

    /// <summary>
    /// Partial match on the normalized TaxNumber, OR-ed with <see cref="NameContains"/>. Null unless the search,
    /// after removing separators, is a non-empty run of digits — so an empty term never matches every NIF.
    /// </summary>
    public string? TaxNumberContains { get; private init; }

    /// <summary>Accepted types; null means no type filter.</summary>
    public IReadOnlyList<PartnerType>? Types { get; private init; }

    public bool IncludeInactive { get; private init; }
    public int Page { get; private init; }
    public int PageSize { get; private init; }

    public int Skip => (Page - 1) * PageSize;

    public static PartnerSearchCriteria Create(string? search, PartnerType? type, bool includeInactive, int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var term = search?.Trim();
        string? nameContains = null, taxNumberContains = null;
        if (!string.IsNullOrEmpty(term))
        {
            nameContains = term;
            var digits = TaxNumber.StripSeparators(term);
            if (digits.Length > 0 && digits.All(char.IsAsciiDigit))
            {
                taxNumberContains = digits;
            }
        }

        return new PartnerSearchCriteria
        {
            NameContains = nameContains,
            TaxNumberContains = taxNumberContains,
            Types = type is { } t ? PartnerTypes.MatchedBy(t) : null,
            IncludeInactive = includeInactive,
            Page = page,
            PageSize = pageSize
        };
    }
}
