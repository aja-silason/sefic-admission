using Partners.Domain.Common;
using Partners.Domain.Partners;

namespace Partners.Application.Partners;

public sealed class ListPartnersUseCase(IPartnerQueryGateway gateway)
{
    public async Task<PagedResponse<PartnerResponse>> ExecuteAsync(ListPartnersInput input, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        PartnerType? type = null;
        if (input.Type is not null)
        {
            if (PartnerTypes.TryParse(input.Type, out var parsed))
            {
                type = parsed;
            }
            else
            {
                errors.Add(nameof(input.Type), PartnerTypes.InvalidMessage);
            }
        }

        if (input.Page < 1)
        {
            errors.Add(nameof(input.Page), "Page must be greater than or equal to 1.");
        }

        if (input.PageSize is < 1 or > ListPartnersInput.MaxPageSize)
        {
            errors.Add(nameof(input.PageSize), $"PageSize must be between 1 and {ListPartnersInput.MaxPageSize}.");
        }

        errors.ThrowIfAny();

        var criteria = PartnerSearchCriteria.Create(input.Search, type, input.IncludeInactive, input.Page, input.PageSize);
        var result = await gateway.SearchAsync(criteria, cancellationToken);

        return new PagedResponse<PartnerResponse>(
            result.Items.Select(PartnerResponse.From).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
    }
}
