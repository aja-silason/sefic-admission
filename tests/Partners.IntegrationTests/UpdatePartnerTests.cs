using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Partners.Application.Partners;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

public sealed class UpdatePartnerTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Scenario4_keeping_own_nif_is_allowed_but_taking_another_partners_nif_is_409_and_changes_nothing()
    {
        var a = await CreateAsync();
        var b = await CreateAsync();

        // Keep its own NIF (even written differently) while changing other data.
        var formattedOwnNif = $"{a.TaxNumber[..3]}.{a.TaxNumber[3..6]}-{a.TaxNumber[6..]}";
        var response = await PutAsync(a.Id, UpdateBody(a, name: "Renamed " + NewMarker(), taxNumber: formattedOwnNif));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(a.TaxNumber, updated.TaxNumber);

        // Take B's NIF: 409 and A keeps every previous value.
        var conflict = await PutAsync(a.Id, UpdateBody(updated, name: "Should not persist", taxNumber: b.TaxNumber));
        await AssertProblemAsync(conflict, HttpStatusCode.Conflict);

        var current = (await (await GetAsync(a.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(updated, current);
    }

    [Fact]
    public async Task Put_returns_new_version_preserves_created_at_and_updates_updated_at()
    {
        var created = await CreateAsync(type: "CUSTOMER");

        var response = await PutAsync(created.Id, new
        {
            name = "  Updated " + NewMarker() + " ",
            taxNumber = created.TaxNumber,
            type = "BOTH",
            email = "  new@example.com ",
            creditLimit = 999999.99m,
            isActive = true,
            version = created.Version,
            // Not editable: must be ignored.
            id = Guid.NewGuid(),
            createdAt = "2000-01-01T00:00:00Z"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("BOTH", updated.Type);
        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal(999999.99m, updated.CreditLimit);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);
        Assert.NotEqual(created.Version, updated.Version);
        Assert.Equal(updated, (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!);
    }

    [Fact]
    public async Task Email_can_be_cleared_with_null()
    {
        var created = await CreateAsync(email: "ana@example.com");

        var response = await PutAsync(created.Id, new
        {
            name = created.Name,
            taxNumber = created.TaxNumber,
            type = created.Type,
            email = (string?)null,
            creditLimit = created.CreditLimit,
            isActive = true,
            version = created.Version
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<PartnerResponse>())!.Email);
    }

    [Fact]
    public async Task Missing_fields_return_400_and_change_nothing()
    {
        var created = await CreateAsync();

        // No isActive / email / version properties at all.
        var response = await PutAsync(created.Id, new
        {
            name = "Partial " + NewMarker(),
            taxNumber = created.TaxNumber,
            type = created.Type,
            creditLimit = created.CreditLimit
        });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(created, (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!);
    }

    [Fact]
    public async Task Invalid_values_return_400_with_all_errors_and_change_nothing()
    {
        var created = await CreateAsync();

        var response = await PutAsync(created.Id, new
        {
            name = "X",
            taxNumber = created.TaxNumber,
            type = created.Type,
            email = (string?)null,
            creditLimit = 10.999m,
            isActive = false,
            version = created.Version
        });

        var problem = (ValidationProblemDetails)await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(["CreditLimit", "Name"], problem.Errors.Keys.Order());
        Assert.Equal(created, (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!);
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var created = await CreateAsync();
        await AssertProblemAsync(await PutAsync(Guid.NewGuid(), UpdateBody(created)), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stale_version_returns_409_and_does_not_overwrite()
    {
        var created = await CreateAsync();
        var first = await UpdateAsync(created, name: "First " + NewMarker());

        // Second writer still holds the original version.
        var stale = await PutAsync(created.Id, UpdateBody(created, name: "Stale writer"));
        var problem = await AssertProblemAsync(stale, HttpStatusCode.Conflict);
        Assert.Contains("Version", problem.Detail);

        Assert.Equal(first, (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!);
    }
}
