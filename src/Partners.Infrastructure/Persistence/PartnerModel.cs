namespace Partners.Infrastructure.Persistence;

/// <summary>EF Core persistence model of the Partners table. Never leaves the infrastructure layer.</summary>
public sealed class PartnerModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string TaxNumber { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Email { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] Version { get; set; } = [];
}
