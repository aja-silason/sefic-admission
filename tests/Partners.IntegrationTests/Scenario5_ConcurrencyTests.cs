using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Partners.Application.Partners;
using Partners.Domain.Partners;
using Partners.Infrastructure.Persistence;
using Partners.IntegrationTests.Infrastructure;

namespace Partners.IntegrationTests;

/// <summary>
/// Scenario 5: simultaneous requests on real SQL Server, each with its own HttpClient / DI scope /
/// DbContext / connection. A gate releases all requests at the same moment to maximize contention.
/// </summary>
public sealed class Scenario5_ConcurrencyTests(SqlServerFixture fixture) : ApiTestBase(fixture)
{
    private const int Parallelism = 8;

    private static async Task<T[]> RunSimultaneouslyAsync<T>(int count, Func<int, Task<T>> action)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, count)
            .Select(i => Task.Run(async () => { await gate.Task; return await action(i); }))
            .ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Simultaneous_creates_with_the_same_nif_produce_one_201_and_409_for_the_rest()
    {
        var digits = NewTaxNumber();
        var clients = Enumerable.Range(0, Parallelism).Select(_ => Fixture.Factory.CreateClient()).ToArray();

        // Same NIF, written in different ways.
        string[] formats = [digits, $"{digits[..3]} {digits[3..6]} {digits[6..]}", $"{digits[..3]}.{digits[3..6]}.{digits[6..]}"];
        var statuses = await RunSimultaneouslyAsync(Parallelism, async i =>
            (await clients[i].PostAsJsonAsync("/api/partners", NewPartnerBody(taxNumber: formats[i % formats.Length]))).StatusCode);

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(Parallelism - 1, statuses.Count(s => s == HttpStatusCode.Conflict));

        await using var db = Fixture.CreateDbContext();
        Assert.Equal(1, await db.Partners.CountAsync(p => p.TaxNumber == digits));
    }

    [Fact]
    public async Task Simultaneous_updates_with_the_same_version_produce_one_200_and_one_409()
    {
        var created = await CreateAsync();
        var clients = new[] { Fixture.Factory.CreateClient(), Fixture.Factory.CreateClient() };
        var names = new[] { "Writer A " + NewMarker(), "Writer B " + NewMarker() };

        var responses = await RunSimultaneouslyAsync(2, i =>
            clients[i].PutAsJsonAsync($"/api/partners/{created.Id}", UpdateBody(created, name: names[i])));

        var statuses = responses.Select(r => r.StatusCode).ToArray();
        Assert.Contains(HttpStatusCode.OK, statuses);
        Assert.Contains(HttpStatusCode.Conflict, statuses);

        // The stored state is exactly the winner's; the loser overwrote nothing.
        var winner = (await responses.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<PartnerResponse>())!;
        var stored = (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(winner, stored);
        Assert.Contains(stored.Name, names);
    }

    [Fact]
    public async Task Repositories_on_independent_contexts_cannot_both_commit_the_same_version()
    {
        var created = await CreateAsync();
        var expected = Convert.FromBase64String(created.Version);

        var outcomes = await RunSimultaneouslyAsync(2, async i =>
        {
            await using var db = Fixture.CreateDbContext();
            var repository = new SqlServerPartnerRepository(db);
            var partner = (await repository.GetByIdAsync(created.Id, CancellationToken.None))!;
            partner.Update(PartnerDetails.Create($"Repo {i}", partner.TaxNumber.Value, "BOTH", null, 1m), true, DateTime.UtcNow);
            try
            {
                await repository.UpdateAsync(partner, expected, CancellationToken.None);
                return "committed";
            }
            catch (PartnerConcurrencyException)
            {
                return "conflict";
            }
        });

        Assert.Equal(["committed", "conflict"], outcomes.Order());
    }

    [Fact]
    public async Task Version_is_checked_by_sql_server_at_write_time_not_only_when_read()
    {
        var created = await CreateAsync();

        await using var db = Fixture.CreateDbContext();
        var repository = new SqlServerPartnerRepository(db);
        var loaded = (await repository.GetByIdAsync(created.Id, CancellationToken.None))!;

        // Another connection changes the row after our read: the version we hold is now stale,
        // even though it matched when loaded.
        await using (var other = Fixture.CreateDbContext())
        {
            await other.Database.ExecuteSqlAsync($"UPDATE Partners SET CreditLimit = 42 WHERE Id = {created.Id}");
        }

        loaded.Update(PartnerDetails.Create("Late writer", loaded.TaxNumber.Value, "CUSTOMER", null, 7m), true, DateTime.UtcNow);
        await Assert.ThrowsAsync<PartnerConcurrencyException>(() =>
            repository.UpdateAsync(loaded, loaded.Version, CancellationToken.None));

        var stored = (await (await GetAsync(created.Id)).Content.ReadFromJsonAsync<PartnerResponse>())!;
        Assert.Equal(42m, stored.CreditLimit);
        Assert.Equal(created.Name, stored.Name);
    }
}
