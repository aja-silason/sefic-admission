using System.Net;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

/// <summary>Scenario 1: normalization of Name/NIF and NIF uniqueness, also after deactivation.</summary>
public sealed class Scenario1_NormalizationAndUniquenessTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Normalizes_name_and_tax_number_and_keeps_the_nif_reserved_after_deactivation()
    {
        var created = await CreateAsync(name: "  Ana Silva  ", taxNumber: "001.234.567");

        Assert.Equal("Ana Silva", created.Name);
        Assert.Equal("001234567", created.TaxNumber);

        // Same NIF, different representation.
        var duplicate = await PostAsync(NewPartnerBody(taxNumber: "001 234 567"));
        var problem = await AssertProblemAsync(duplicate, HttpStatusCode.Conflict);
        Assert.Contains("001234567", problem.Detail);

        // Deactivate: the NIF is still taken.
        await UpdateAsync(created, isActive: false);
        await AssertProblemAsync(await PostAsync(NewPartnerBody(taxNumber: "001-234-567")), HttpStatusCode.Conflict);

        await using var db = Fixture.CreateDbContext();
        Assert.Single(db.Partners, p => p.TaxNumber == "001234567");
    }
}
