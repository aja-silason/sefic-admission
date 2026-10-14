using Partners.Domain.Partners;

namespace Partners.UnitTests;

public sealed class PartnerSearchCriteriaTests
{
    private static PartnerSearchCriteria Search(string? term) =>
        PartnerSearchCriteria.Create(term, null, includeInactive: false, page: 1, pageSize: 20);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_search_applies_no_text_filter(string? term)
    {
        var criteria = Search(term);
        Assert.Null(criteria.NameContains);
        Assert.Null(criteria.TaxNumberContains);
    }

    [Fact]
    public void Text_search_never_enables_the_tax_number_branch()
    {
        var criteria = Search("Ana");
        Assert.Equal("Ana", criteria.NameContains);
        Assert.Null(criteria.TaxNumberContains);
    }

    [Theory]
    [InlineData("...")]
    [InlineData(" - ")]
    public void Separator_only_search_does_not_match_every_tax_number(string term)
    {
        var criteria = Search(term);
        Assert.Null(criteria.TaxNumberContains);
        Assert.Equal(term.Trim(), criteria.NameContains);
    }

    [Theory]
    [InlineData("001.234", "001234")]
    [InlineData(" 234 567 ", "234567")]
    [InlineData("4-5", "45")]
    public void Numeric_search_is_normalized_for_the_tax_number_branch(string term, string expected) =>
        Assert.Equal(expected, Search(term).TaxNumberContains);

    [Theory]
    [InlineData(PartnerType.Customer, new[] { PartnerType.Customer, PartnerType.Both })]
    [InlineData(PartnerType.Supplier, new[] { PartnerType.Supplier, PartnerType.Both })]
    [InlineData(PartnerType.Both, new[] { PartnerType.Both })]
    public void Type_filter_includes_both_where_required(PartnerType filter, PartnerType[] expected) =>
        Assert.Equal(expected, PartnerSearchCriteria.Create(null, filter, false, 1, 20).Types);

    [Fact]
    public void Skip_is_derived_from_page_and_page_size() =>
        Assert.Equal(40, PartnerSearchCriteria.Create(null, null, false, 3, 20).Skip);
}
