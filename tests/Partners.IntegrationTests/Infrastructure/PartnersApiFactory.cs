using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Partners.IntegrationTests.Infrastructure;

public sealed class PartnersApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Partners", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Database:SeedSampleData", "false");
    }
}
