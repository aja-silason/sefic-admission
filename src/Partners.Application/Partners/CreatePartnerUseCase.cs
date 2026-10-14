using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed class CreatePartnerUseCase(IPartnerRepository repository, TimeProvider clock)
{
    public async Task<PartnerResponse> ExecuteAsync(CreatePartnerInput input, CancellationToken cancellationToken)
    {
        var details = PartnerDetails.Create(input.Name, input.TaxNumber, input.Type, input.Email, input.CreditLimit);
        var partner = Partner.Create(details, clock.GetUtcNow().UtcDateTime);

        // No pre-check for an existing NIF: the unique index decides, which is also race-free.
        var saved = await repository.AddAsync(partner, cancellationToken);
        return PartnerResponse.From(saved);
    }
}
