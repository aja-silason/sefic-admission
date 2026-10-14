using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed record CreatePartnerInput(string? Name, string? TaxNumber, string? Type, string? Email, decimal? CreditLimit);

public sealed record UpdatePartnerInput(
    string? Name,
    string? TaxNumber,
    string? Type,
    string? Email,
    decimal? CreditLimit,
    bool? IsActive,
    string? Version);

public sealed record ListPartnersInput(string? Search, string? Type, bool IncludeInactive, int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

/// <summary>Partner as returned by the API. <see cref="Version"/> is the opaque Base64 concurrency token.</summary>
public sealed record PartnerResponse(
    Guid Id,
    string Name,
    string TaxNumber,
    string Type,
    string? Email,
    decimal CreditLimit,
    bool IsActive,
    string Version,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static PartnerResponse From(Partner partner) => new(
        partner.Id,
        partner.Name,
        partner.TaxNumber.Value,
        partner.Type.ToCode(),
        partner.Email,
        partner.CreditLimit,
        partner.IsActive,
        VersionToken.Encode(partner.Version),
        partner.CreatedAt,
        partner.UpdatedAt);
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
