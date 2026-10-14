namespace Partners.Domain.Partners;

public enum PartnerType
{
    Customer,
    Supplier,
    Both
}

/// <summary>Public codes of <see cref="PartnerType"/> (CUSTOMER, SUPPLIER, BOTH) and the listing filter rule.</summary>
public static class PartnerTypes
{
    public const string CustomerCode = "CUSTOMER";
    public const string SupplierCode = "SUPPLIER";
    public const string BothCode = "BOTH";

    public static readonly IReadOnlyList<string> Codes = [CustomerCode, SupplierCode, BothCode];

    public static string InvalidMessage => $"Type must be one of: {string.Join(", ", Codes)}.";

    public static string ToCode(this PartnerType type) => type switch
    {
        PartnerType.Customer => CustomerCode,
        PartnerType.Supplier => SupplierCode,
        PartnerType.Both => BothCode,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Strict parse: only the exact upper-case codes are accepted (no numbers, no aliases).</summary>
    public static bool TryParse(string? code, out PartnerType type)
    {
        switch (code)
        {
            case CustomerCode: type = PartnerType.Customer; return true;
            case SupplierCode: type = PartnerType.Supplier; return true;
            case BothCode: type = PartnerType.Both; return true;
            default: type = default; return false;
        }
    }

    /// <summary>
    /// Types matched by a listing filter: CUSTOMER and SUPPLIER also match BOTH; BOTH matches only BOTH.
    /// </summary>
    public static IReadOnlyList<PartnerType> MatchedBy(PartnerType filter) => filter switch
    {
        PartnerType.Customer => [PartnerType.Customer, PartnerType.Both],
        PartnerType.Supplier => [PartnerType.Supplier, PartnerType.Both],
        PartnerType.Both => [PartnerType.Both],
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
    };
}
