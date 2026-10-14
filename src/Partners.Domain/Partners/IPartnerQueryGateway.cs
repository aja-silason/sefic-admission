namespace Partners.Domain.Partners;

/// <summary>Read-side listing of partners, executed by the store (filters, count and paging).</summary>
public interface IPartnerQueryGateway
{
    Task<PagedResult<Partner>> SearchAsync(PartnerSearchCriteria criteria, CancellationToken cancellationToken);
}
