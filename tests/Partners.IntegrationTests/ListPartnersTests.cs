using System.Data.SqlTypes;
using System.Net;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

public sealed class ListPartnersTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Scenario2_both_appears_in_every_type_filter_and_customer_not_in_supplier()
    {
        var marker = NewMarker();
        var both = await CreateAsync(name: $"Both {marker}", type: "BOTH");
        var customer = await CreateAsync(name: $"Customer {marker}", type: "CUSTOMER");
        var supplier = await CreateAsync(name: $"Supplier {marker}", type: "SUPPLIER");

        async Task<Guid[]> Ids(string type) =>
            (await ListAsync($"search={marker}&type={type}")).Items.Select(p => p.Id).ToArray();

        Assert.Equal([both.Id, customer.Id], await Ids("CUSTOMER"));
        Assert.Equal([both.Id, supplier.Id], await Ids("SUPPLIER"));
        Assert.Equal([both.Id], await Ids("BOTH"));
    }

    [Fact]
    public async Task Scenario3_deactivated_partner_leaves_default_listing_and_returns_when_reactivated()
    {
        var marker = NewMarker();
        var partner = await CreateAsync(name: $"Toggle {marker}");

        var inactive = await UpdateAsync(partner, isActive: false);

        Assert.Empty((await ListAsync($"search={marker}")).Items);
        var withInactive = await ListAsync($"search={marker}&includeInactive=true");
        Assert.Equal(partner.Id, Assert.Single(withInactive.Items).Id);
        Assert.False(withInactive.Items[0].IsActive);

        var byId = await GetAsync(partner.Id);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

        await UpdateAsync(inactive, isActive: true);
        Assert.Equal(partner.Id, Assert.Single((await ListAsync($"search={marker}")).Items).Id);
    }

    [Fact]
    public async Task Scenario7_combined_filters_count_before_paging_and_tie_break_by_id()
    {
        var marker = NewMarker();
        var sameName = $"Same {marker}";
        var tied = new[]
        {
            await CreateAsync(name: sameName, type: "CUSTOMER"),
            await CreateAsync(name: sameName, type: "BOTH"),
            await CreateAsync(name: sameName, type: "CUSTOMER")
        };
        await CreateAsync(name: $"Other {marker}", type: "SUPPLIER");               // excluded by type
        var inactive = await CreateAsync(name: $"Same {marker} old", type: "CUSTOMER");
        await UpdateAsync(inactive, isActive: false);                                  // excluded by state

        // Combined filters (search AND type AND active) with a page smaller than the result.
        var page1 = await ListAsync($"search={marker}&type=CUSTOMER&page=1&pageSize=2");
        var page2 = await ListAsync($"search={marker}&type=CUSTOMER&page=2&pageSize=2");
        var page3 = await ListAsync($"search={marker}&type=CUSTOMER&page=3&pageSize=2");

        Assert.Equal(3, page1.TotalCount);           // counted before paging
        Assert.Equal(3, page2.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.Empty(page3.Items);                    // beyond the results
        Assert.Equal(3, page3.TotalCount);
        Assert.Equal((2, 2), (page2.Page, page2.PageSize));

        // Equal names are ordered by Id, as SQL Server orders uniqueidentifier values.
        var expectedOrder = tied.Select(p => p.Id).OrderBy(id => new SqlGuid(id)).ToArray();
        Assert.Equal(expectedOrder, page1.Items.Concat(page2.Items).Select(p => p.Id).ToArray());

        // Including inactive adds the deactivated one, which sorts after "Same {marker}".
        var all = await ListAsync($"search={marker}&type=CUSTOMER&includeInactive=true");
        Assert.Equal(4, all.TotalCount);
        Assert.Equal(inactive.Id, all.Items[^1].Id);
    }

    [Fact]
    public async Task Scenario7_text_search_does_not_match_every_partner_through_the_tax_number_branch()
    {
        var marker = NewMarker();
        var ana = await CreateAsync(name: $"Ana {marker}");
        var bruno = await CreateAsync(name: $"Bruno {marker}");

        var result = await ListAsync("search=Ana&includeInactive=true&pageSize=100");

        Assert.Contains(result.Items, p => p.Id == ana.Id);
        Assert.DoesNotContain(result.Items, p => p.Id == bruno.Id);
        Assert.All(result.Items, p => Assert.Contains("ana", p.Name, StringComparison.OrdinalIgnoreCase));
        Assert.True(result.TotalCount < await CountRowsAsync());
    }

    [Fact]
    public async Task Name_search_is_partial_and_case_insensitive()
    {
        var marker = NewMarker();
        var partner = await CreateAsync(name: $"Distribuidora {marker} Lda");

        var result = await ListAsync($"search={marker.ToUpperInvariant()[2..]}");

        Assert.Equal(partner.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task Tax_number_search_normalizes_separators()
    {
        var partner = await CreateAsync(taxNumber: NewTaxNumber());
        var nif = partner.TaxNumber;
        var term = Uri.EscapeDataString($"{nif[1..4]}.{nif[4..6]} {nif[6..8]}");

        var result = await ListAsync($"search={term}&pageSize=100");

        Assert.Contains(result.Items, p => p.Id == partner.Id);
        Assert.All(result.Items, p => Assert.Contains(nif[1..8], p.TaxNumber));
    }

    [Theory]
    [InlineData("---")]
    [InlineData(". .")]
    public async Task Separator_only_search_does_not_match_every_tax_number(string term)
    {
        await CreateAsync();
        Assert.Equal(0, (await ListAsync($"search={Uri.EscapeDataString(term)}&includeInactive=true")).TotalCount);
    }

    [Fact]
    public async Task Like_wildcards_in_search_are_literal()
    {
        await CreateAsync();
        var result = await ListAsync($"search={Uri.EscapeDataString("%")}&includeInactive=true");
        Assert.All(result.Items, p => Assert.Contains("%", p.Name));
    }

    [Fact]
    public async Task Defaults_are_page_1_size_20_active_only()
    {
        var result = await ListAsync("");
        Assert.Equal((1, 20), (result.Page, result.PageSize));
        Assert.True(result.Items.Count <= 20);
        Assert.All(result.Items, p => Assert.True(p.IsActive));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("type=VENDOR")]
    [InlineData("type=customer")]
    [InlineData("page=abc")]
    [InlineData("includeInactive=maybe")]
    public async Task Invalid_filters_return_400(string query) =>
        await AssertProblemAsync(await Client.GetAsync($"/api/partners?{query}"), HttpStatusCode.BadRequest);
}
