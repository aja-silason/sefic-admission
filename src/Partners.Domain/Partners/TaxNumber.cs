namespace Partners.Domain.Partners;

/// <summary>
/// Normalized tax number (NIF): exactly nine ASCII digits stored as text, so leading zeros survive.
/// Simplified exercise rule — no real fiscal check-digit validation.
/// </summary>
public sealed record TaxNumber
{
    public const int Length = 9;

    private TaxNumber(string value) => Value = value;

    public string Value { get; }

    /// <summary>Removes the accepted separators: spaces, dots and hyphens.</summary>
    public static string StripSeparators(string raw) =>
        string.Create(raw.Length - raw.Count(IsSeparator), raw, static (span, source) =>
        {
            var i = 0;
            foreach (var c in source)
            {
                if (!IsSeparator(c)) span[i++] = c;
            }
        });

    public static bool TryCreate(string? raw, out TaxNumber? taxNumber, out string? error)
    {
        taxNumber = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "TaxNumber is required.";
            return false;
        }

        var normalized = StripSeparators(raw);
        if (normalized.Length != Length || !normalized.All(char.IsAsciiDigit))
        {
            error = "TaxNumber must contain exactly 9 digits (spaces, dots and hyphens are ignored).";
            return false;
        }

        error = null;
        taxNumber = new TaxNumber(normalized);
        return true;
    }

    /// <summary>Rehydrates an already-normalized value coming from persistence.</summary>
    public static TaxNumber FromTrusted(string normalized) => new(normalized);

    public override string ToString() => Value;

    private static bool IsSeparator(char c) => c is ' ' or '.' or '-';
}
