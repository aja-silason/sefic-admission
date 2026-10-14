using Partners.Application.Partners;

namespace Partners.Api.Contracts;

/// <summary>Body of POST /api/partners. Missing or invalid fields are reported together with 400.</summary>
public sealed record CreatePartnerRequest
{
    /// <example>Ana Silva</example>
    public string? Name { get; init; }

    /// <summary>Nine digits; spaces, dots and hyphens are ignored.</summary>
    /// <example>001.234.567</example>
    public string? TaxNumber { get; init; }

    /// <summary>CUSTOMER, SUPPLIER or BOTH.</summary>
    /// <example>CUSTOMER</example>
    public string? Type { get; init; }

    /// <example>ana.silva@example.com</example>
    public string? Email { get; init; }

    /// <summary>Between 0 and 1 000 000, at most two decimal places.</summary>
    /// <example>1500.50</example>
    public decimal? CreditLimit { get; init; }

    public CreatePartnerInput ToInput() => new(Name, TaxNumber, Type, Email, CreditLimit);
}

/// <summary>
/// Body of PUT /api/partners/{id}: a full replacement. Every property must be present in the JSON
/// (Email may be null). Id, CreatedAt and UpdatedAt are not editable and are ignored if sent.
/// </summary>
public sealed record UpdatePartnerRequest
{
    public required string? Name { get; init; }
    public required string? TaxNumber { get; init; }
    public required string? Type { get; init; }
    public required string? Email { get; init; }
    public required decimal? CreditLimit { get; init; }
    public required bool? IsActive { get; init; }

    /// <summary>The Version returned by the last read or write of this partner.</summary>
    public required string? Version { get; init; }

    public UpdatePartnerInput ToInput() => new(Name, TaxNumber, Type, Email, CreditLimit, IsActive, Version);
}

/// <summary>Query string of GET /api/partners.</summary>
public sealed record ListPartnersRequest
{
    /// <summary>Partial, case-insensitive match on Name, or on TaxNumber (separators ignored).</summary>
    public string? Search { get; init; }

    /// <summary>CUSTOMER (includes BOTH), SUPPLIER (includes BOTH) or BOTH.</summary>
    public string? Type { get; init; }

    public bool IncludeInactive { get; init; }
    public int Page { get; init; } = ListPartnersInput.DefaultPage;
    public int PageSize { get; init; } = ListPartnersInput.DefaultPageSize;

    public ListPartnersInput ToInput() => new(Search, Type, IncludeInactive, Page, PageSize);
}
