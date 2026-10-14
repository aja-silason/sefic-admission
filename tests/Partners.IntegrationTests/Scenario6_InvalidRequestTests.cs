using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Partners.Application.Partners;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

/// <summary>Scenario 6: invalid input returns 400 and leaves the database untouched.</summary>
public sealed class Scenario6_InvalidRequestTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Credit_limit_with_three_decimals_returns_400_without_rounding_or_inserting()
    {
        var rows = await CountRowsAsync();
        var taxNumber = NewTaxNumber();

        var problem = (ValidationProblemDetails)await AssertProblemAsync(
            await PostAsync(NewPartnerBody(taxNumber: taxNumber, creditLimit: 10.999m)), HttpStatusCode.BadRequest);

        Assert.Contains("CreditLimit", problem.Errors.Keys);
        Assert.Equal(rows, await CountRowsAsync());
        Assert.Equal(0, (await ListAsync($"search={taxNumber}&includeInactive=true")).TotalCount);
    }

    [Theory]
    [InlineData("12345678A")]
    [InlineData("ABC.DEF.GHI")]
    [InlineData("PT123456789")]
    public async Task Tax_number_with_letters_returns_400_without_inserting(string taxNumber)
    {
        var rows = await CountRowsAsync();

        var problem = (ValidationProblemDetails)await AssertProblemAsync(
            await PostAsync(NewPartnerBody(taxNumber: taxNumber)), HttpStatusCode.BadRequest);

        Assert.Contains("TaxNumber", problem.Errors.Keys);
        Assert.Equal(rows, await CountRowsAsync());
    }

    [Fact]
    public async Task Invalid_type_returns_400_on_create_and_on_list()
    {
        var rows = await CountRowsAsync();

        await AssertProblemAsync(await PostAsync(NewPartnerBody(type: "PARTNER")), HttpStatusCode.BadRequest);
        await AssertProblemAsync(await Client.GetAsync("/api/partners?type=PARTNER"), HttpStatusCode.BadRequest);

        Assert.Equal(rows, await CountRowsAsync());
    }

    [Fact]
    public async Task Page_zero_returns_400() =>
        await AssertProblemAsync(await Client.GetAsync("/api/partners?page=0"), HttpStatusCode.BadRequest);

    [Theory]
    [InlineData("not base64!")]
    [InlineData("AAAA")]
    [InlineData("")]
    public async Task Malformed_version_returns_400_and_changes_nothing(string version)
    {
        var created = await CreateAsync();

        var problem = (ValidationProblemDetails)await AssertProblemAsync(
            await PutAsync(created.Id, UpdateBody(created, name: "Never stored", isActive: false, version: version)),
            HttpStatusCode.BadRequest);

        Assert.Contains("Version", problem.Errors.Keys);
        var stored = (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(created, stored);
    }

    [Fact]
    public async Task Null_version_returns_400()
    {
        var created = await CreateAsync();
        var body = new
        {
            name = created.Name,
            taxNumber = created.TaxNumber,
            type = created.Type,
            email = created.Email,
            creditLimit = created.CreditLimit,
            isActive = true,
            version = (string?)null
        };

        await AssertProblemAsync(await PutAsync(created.Id, body), HttpStatusCode.BadRequest);
    }
}
