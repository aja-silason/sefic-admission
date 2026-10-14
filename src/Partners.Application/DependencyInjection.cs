using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Partners.Application.Partners;

namespace Partners.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<CreatePartnerUseCase>();
        services.AddScoped<GetPartnerUseCase>();
        services.AddScoped<UpdatePartnerUseCase>();
        services.AddScoped<ListPartnersUseCase>();
        return services;
    }
}
