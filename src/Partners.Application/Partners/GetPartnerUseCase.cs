using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed class GetPartnerUseCase(IPartnerRepository repository)
{
    /// <summary>Returns the partner whether active or not.</summary>
    public async Task<PartnerResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var partner = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new PartnerNotFoundException(id);
        return PartnerResponse.From(partner);
    }
}
