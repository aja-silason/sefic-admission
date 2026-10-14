namespace Partners.Domain.Partners;

/// <summary>
/// A business partner of the company: customer, supplier or both. One record per tax number.
/// State changes only through <see cref="Create"/>, <see cref="Update"/> and <see cref="From"/> (rehydration).
/// </summary>
public sealed class Partner
{
    private Partner(Guid id, PartnerDetails details, bool isActive, DateTime createdAt, DateTime updatedAt, byte[] version)
    {
        Id = id;
        Details = details;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Version = version;
    }

    public Guid Id { get; }
    public PartnerDetails Details { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Concurrency version assigned by the store; empty until the partner is persisted.</summary>
    public byte[] Version { get; }

    public string Name => Details.Name;
    public TaxNumber TaxNumber => Details.TaxNumber;
    public PartnerType Type => Details.Type;
    public string? Email => Details.Email;
    public decimal CreditLimit => Details.CreditLimit;

    /// <summary>New partners start active, with both timestamps set to the server's UTC clock.</summary>
    public static Partner Create(PartnerDetails details, DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return new Partner(Guid.NewGuid(), details, isActive: true, utcNow, utcNow, []);
    }

    /// <summary>Full replacement of the editable data. Id and CreatedAt never change.</summary>
    public void Update(PartnerDetails details, bool isActive, DateTime utcNow)
    {
        EnsureUtc(utcNow);
        Details = details;
        IsActive = isActive;
        UpdatedAt = utcNow;
    }

    /// <summary>Rebuilds a partner from persisted state that was validated when written.</summary>
    public static Partner From(
        Guid id,
        string name,
        string taxNumber,
        PartnerType type,
        string? email,
        decimal creditLimit,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt,
        byte[] version) =>
        new(id,
            PartnerDetails.FromTrusted(name, TaxNumber.FromTrusted(taxNumber), type, email, creditLimit),
            isActive,
            DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc),
            version);

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamps must be UTC.", nameof(value));
        }
    }
}
