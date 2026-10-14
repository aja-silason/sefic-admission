using System.Net;
using System.Net.Http.Json;
using Partners.Application.Partners;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

public sealed class CreateAndGetPartnerTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Post_returns_201_with_location_id_version_and_server_fields()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var response = await PostAsync(new
        {
            name = "Created " + NewMarker(),
            taxNumber = NewTaxNumber(),
            type = "SUPPLIER",
            email = "  compras@example.com ",
            creditLimit = 1500.50m
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal($"/api/partners/{created.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("SUPPLIER", created.Type);
        Assert.Equal("compras@example.com", created.Email);
        Assert.Equal(1500.50m, created.CreditLimit);
        Assert.True(created.IsActive);
        Assert.True(VersionToken.TryDecode(created.Version, out _));
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.InRange(created.CreatedAt, before, DateTime.UtcNow.AddSeconds(1));
        Assert.Equal(created.CreatedAt, created.UpdatedAt);

        var fetched = (await Client.GetFromJsonAsync<PartnerResponse>(response.Headers.Location))!;
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Post_ignores_server_controlled_fields_sent_by_the_client()
    {
        var forcedId = Guid.NewGuid();
        var response = await PostAsync(new
        {
            id = forcedId,
            name = "Forced " + NewMarker(),
            taxNumber = NewTaxNumber(),
            type = "CUSTOMER",
            creditLimit = 0,
            isActive = false,
            createdAt = "2000-01-01T00:00:00Z"
        });

        var created = (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.NotEqual(forcedId, created.Id);
        Assert.True(created.IsActive);
        Assert.True(created.CreatedAt > new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Null(created.Email);
    }

    [Fact]
    public async Task Get_unknown_id_returns_404_problem()
    {
        var problem = await AssertProblemAsync(await GetAsync(Guid.NewGuid()), HttpStatusCode.NotFound);
        Assert.Contains("not found", problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_malformed_id_returns_400() =>
        await AssertProblemAsync(await Client.GetAsync("/api/partners/not-a-guid"), HttpStatusCode.BadRequest);

    [Fact]
    public async Task Malformed_json_returns_400_problem() =>
        await AssertProblemAsync(
            await Client.PostAsync("/api/partners", new StringContent("{ \"name\": ", System.Text.Encoding.UTF8, "application/json")),
            HttpStatusCode.BadRequest);

    [Fact]
    public async Task Post_reports_all_invalid_fields_and_writes_nothing()
    {
        var rowsBefore = await CountRowsAsync();

        var response = await PostAsync(new { name = " ", taxNumber = "12AB", type = "VENDOR", email = "x@", creditLimit = -1 });

        var problem = (Microsoft.AspNetCore.Mvc.ValidationProblemDetails)await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(["CreditLimit", "Email", "Name", "TaxNumber", "Type"], problem.Errors.Keys.Order());
        Assert.Equal(rowsBefore, await CountRowsAsync());
    }
}
