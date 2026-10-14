namespace Partners.Domain.Partners;

/// <summary>A page of results plus the total count of matches before paging.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
