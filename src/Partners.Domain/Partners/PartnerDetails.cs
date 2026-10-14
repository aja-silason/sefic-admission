using System.Text.RegularExpressions;
using Partners.Domain.Common;

namespace Partners.Domain.Partners;

/// <summary>
/// The client-editable data of a partner, normalized and validated as a whole.
/// Can only be obtained through <see cref="Create"/>, so an instance is always valid.
/// </summary>
public sealed partial class PartnerDetails
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const decimal CreditLimitMin = 0m;
    public const decimal CreditLimitMax = 1_000_000m;
    public const int CreditLimitDecimals = 2;

    private PartnerDetails(string name, TaxNumber taxNumber, PartnerType type, string? email, decimal creditLimit)
    {
        Name = name;
        TaxNumber = taxNumber;
        Type = type;
        Email = email;
        CreditLimit = creditLimit;
    }

    public string Name { get; }
    public TaxNumber TaxNumber { get; }
    public PartnerType Type { get; }
    public string? Email { get; }
    public decimal CreditLimit { get; }

    /// <summary>Normalizes and validates every field, throwing one <see cref="DomainValidationException"/> with all failures.</summary>
    public static PartnerDetails Create(string? name, string? taxNumber, string? type, string? email, decimal? creditLimit)
    {
        var errors = new ValidationErrors();

        var normalizedName = NormalizeName(name, errors);

        if (!TaxNumber.TryCreate(taxNumber, out var normalizedTaxNumber, out var taxError))
        {
            errors.Add(nameof(TaxNumber), taxError!);
        }

        var parsedType = ParseType(type, errors);
        var normalizedEmail = NormalizeEmail(email, errors);
        ValidateCreditLimit(creditLimit, errors);

        errors.ThrowIfAny();

        return new PartnerDetails(normalizedName!, normalizedTaxNumber!, parsedType, normalizedEmail, creditLimit!.Value);
    }

    internal static PartnerDetails FromTrusted(string name, TaxNumber taxNumber, PartnerType type, string? email, decimal creditLimit) =>
        new(name, taxNumber, type, email, creditLimit);

    private static string? NormalizeName(string? name, ValidationErrors errors)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            errors.Add(nameof(Name), "Name is required.");
            return null;
        }

        if (trimmed.Length is < NameMinLength or > NameMaxLength)
        {
            errors.Add(nameof(Name), $"Name must be between {NameMinLength} and {NameMaxLength} characters after trimming.");
            return null;
        }

        return trimmed;
    }

    private static PartnerType ParseType(string? type, ValidationErrors errors)
    {
        if (type is null)
        {
            errors.Add(nameof(Type), "Type is required.");
        }
        else if (!PartnerTypes.TryParse(type, out var parsed))
        {
            errors.Add(nameof(Type), PartnerTypes.InvalidMessage);
        }
        else
        {
            return parsed;
        }

        return default;
    }

    private static string? NormalizeEmail(string? email, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength || !EmailPattern().IsMatch(trimmed))
        {
            errors.Add(nameof(Email), "Email is not a valid email address.");
            return null;
        }

        return trimmed;
    }

    private static void ValidateCreditLimit(decimal? creditLimit, ValidationErrors errors)
    {
        if (creditLimit is not { } value)
        {
            errors.Add(nameof(CreditLimit), "CreditLimit is required.");
            return;
        }

        if (value is < CreditLimitMin or > CreditLimitMax)
        {
            errors.Add(nameof(CreditLimit), $"CreditLimit must be between {CreditLimitMin} and {CreditLimitMax:0}.");
        }

        // Excess precision is rejected, never rounded silently (10.990 is fine, 10.999 is not).
        if (decimal.Round(value, CreditLimitDecimals) != value)
        {
            errors.Add(nameof(CreditLimit), $"CreditLimit must have at most {CreditLimitDecimals} decimal places.");
        }
    }

    // Pragmatic address check: ASCII local part without leading/trailing/double dots, domain labels and a TLD.
    [GeneratedRegex(
        @"^(?!\.)(?!.*\.\.)[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+(?<!\.)@[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*\.[A-Za-z]{2,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
