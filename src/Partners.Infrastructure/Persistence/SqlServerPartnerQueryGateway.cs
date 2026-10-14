using Microsoft.EntityFrameworkCore;
using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence;

/// <summary>Listing translated entirely to SQL: WHERE, COUNT, ORDER BY and OFFSET/FETCH run on SQL Server.</summary>
internal sealed class SqlServerPartnerQueryGateway(PartnersDbContext db) : IPartnerQueryGateway
{
    public async Task<PagedResult<Partner>> SearchAsync(PartnerSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var query = db.Partners.AsNoTracking();

        if (!criteria.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (criteria.Types is { } types)
        {
            var codes = types.Select(t => t.ToCode()).ToArray();
            query = query.Where(p => codes.Contains(p.Type));
        }

        if (criteria.NameContains is { } name)
        {
            // string.Contains becomes LIKE with escaped wildcards; the Name collation makes it case-insensitive.
            query = criteria.TaxNumberContains is { } taxNumber
                ? query.Where(p => p.Name.Contains(name) || p.TaxNumber.Contains(taxNumber))
                : query.Where(p => p.Name.Contains(name));
        }

        // Total of the filtered set, counted before paging.
        var totalCount = await query.CountAsync(cancellationToken);

        List<PartnerModel> page = [];
        if (criteria.Skip < totalCount)
        {
            page = await query
                .OrderBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Skip(criteria.Skip)
                .Take(criteria.PageSize)
                .ToListAsync(cancellationToken);
        }

        return new PagedResult<Partner>(
            page.Select(m => m.ToDomain()).ToList(),
            totalCount,
            criteria.Page,
            criteria.PageSize);
    }
}
