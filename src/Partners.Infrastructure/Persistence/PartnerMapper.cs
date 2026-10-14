using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence;

internal static class PartnerMapper
{
    public static Partner ToDomain(this PartnerModel model)
    {
        if (!PartnerTypes.TryParse(model.Type, out var type))
        {
            throw new InvalidOperationException($"Unknown partner type '{model.Type}' stored for partner '{model.Id}'.");
        }

        return Partner.From(
            model.Id,
            model.Name,
            model.TaxNumber,
            type,
            model.Email,
            model.CreditLimit,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt,
            model.Version);
    }

    public static PartnerModel ToModel(this Partner partner) => new()
    {
        Id = partner.Id,
        Name = partner.Name,
        TaxNumber = partner.TaxNumber.Value,
        Type = partner.Type.ToCode(),
        Email = partner.Email,
        CreditLimit = partner.CreditLimit,
        IsActive = partner.IsActive,
        CreatedAt = partner.CreatedAt,
        UpdatedAt = partner.UpdatedAt,
        Version = partner.Version
    };
}
