using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Partners.Domain.Partners;
using Partners.Infrastructure.Persistence;

namespace Partners.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Partners";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PartnersDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
            options.UseSqlServer(connectionString);
        });
        services.AddScoped<IPartnerRepository, SqlServerPartnerRepository>();
        services.AddScoped<IPartnerQueryGateway, SqlServerPartnerQueryGateway>();
        return services;
    }
}
