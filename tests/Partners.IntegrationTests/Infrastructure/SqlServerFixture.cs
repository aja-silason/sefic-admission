using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Partners.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Partners.IntegrationTests.Infrastructure;

/// <summary>
/// One real SQL Server for the whole test run, with a fresh database migrated through the EF Core migrations.
/// Uses Docker (Testcontainers) unless PARTNERS_TEST_SQLSERVER points to an existing server.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ExternalServerVariable = "PARTNERS_TEST_SQLSERVER";

    private MsSqlContainer? _container;

    public string ConnectionString { get; private set; } = null!;
    public PartnersApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var serverConnectionString = Environment.GetEnvironmentVariable(ExternalServerVariable);
        if (string.IsNullOrWhiteSpace(serverConnectionString))
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();
            serverConnectionString = _container.GetConnectionString();
        }

        ConnectionString = new SqlConnectionStringBuilder(serverConnectionString)
        {
            InitialCatalog = $"PartnersTests_{Guid.NewGuid():N}",
            TrustServerCertificate = true
        }.ConnectionString;

        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        Factory = new PartnersApiFactory(ConnectionString);
    }

    /// <summary>A new context on its own connection, independent from the API's.</summary>
    public PartnersDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<PartnersDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();

        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        await using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}
