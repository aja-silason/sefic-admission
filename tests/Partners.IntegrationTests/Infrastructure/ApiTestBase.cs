using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Partners.Application.Partners;

namespace Partners.IntegrationTests.Infrastructure;

[Collection(SqlServerCollection.Name)]
public abstract class ApiTestBase(SqlServerFixture fixture)
{
    protected SqlServerFixture Fixture { get; } = fixture;
    protected HttpClient Client { get; } = fixture.Factory.CreateClient();

    /// <summary>Random 9-digit NIF, so tests sharing the database never collide.</summary>
    protected static string NewTaxNumber() => Random.Shared.NextInt64(100_000_000, 1_000_000_000).ToString();

    /// <summary>Unique marker to put in names, so searches only see the current test's rows.</summary>
    protected static string NewMarker() => "t" + Guid.NewGuid().ToString("N")[..10];

    protected static object NewPartnerBody(
        string? name = null,
        string? taxNumber = null,
        string type = "CUSTOMER",
        string? email = null,
        decimal creditLimit = 100m) =>
        new { name = name ?? "Partner " + NewMarker(), taxNumber = taxNumber ?? NewTaxNumber(), type, email, creditLimit };

    protected Task<HttpResponseMessage> PostAsync(object body) => Client.PostAsJsonAsync("/api/partners", body);

    protected async Task<PartnerResponse> CreateAsync(
        string? name = null,
        string? taxNumber = null,
        string type = "CUSTOMER",
        string? email = null,
        decimal creditLimit = 100m)
    {
        var response = await PostAsync(NewPartnerBody(name, taxNumber, type, email, creditLimit));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
    }

    protected Task<HttpResponseMessage> GetAsync(Guid id) => Client.GetAsync($"/api/partners/{id}");

    protected Task<HttpResponseMessage> PutAsync(Guid id, object body) => Client.PutAsJsonAsync($"/api/partners/{id}", body);

    /// <summary>Full PUT body from the current state, with optional changes.</summary>
    protected static object UpdateBody(
        PartnerResponse current,
        string? name = null,
        string? taxNumber = null,
        string? type = null,
        bool? isActive = null,
        string? version = null) =>
        new
        {
            name = name ?? current.Name,
            taxNumber = taxNumber ?? current.TaxNumber,
            type = type ?? current.Type,
            email = current.Email,
            creditLimit = current.CreditLimit,
            isActive = isActive ?? current.IsActive,
            version = version ?? current.Version
        };

    protected async Task<PartnerResponse> UpdateAsync(PartnerResponse current, bool? isActive = null, string? name = null)
    {
        var response = await PutAsync(current.Id, UpdateBody(current, name: name, isActive: isActive));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PartnerResponse>())!;
    }

    protected async Task<PagedResponse<PartnerResponse>> ListAsync(string query)
    {
        var response = await Client.GetAsync($"/api/partners?{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResponse<PartnerResponse>>())!;
    }

    protected static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body); // no stack trace
        var problem = System.Text.Json.JsonSerializer.Deserialize<ValidationProblemDetails>(
            body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
        Assert.Equal((int)expected, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        return problem;
    }

    protected async Task<int> CountRowsAsync()
    {
        await using var db = Fixture.CreateDbContext();
        return await db.Partners.CountAsync();
    }
}
