using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GovernmentCollections.Domain.Settings;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public static class InterswitchServiceExtensions
{
    public static IServiceCollection AddInterswitchServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("InterswitchSettings").Get<InterswitchSettings>() ?? new InterswitchSettings();
        services.AddSingleton(settings);

        services.AddHttpClient<InterswitchAuthService>();
        services.AddHttpClient<InterswitchServicesService>();
        services.AddHttpClient<InterswitchTransactionService>();

        services.AddScoped<InterswitchAuthService>();
        services.AddScoped<InterswitchServicesService>();
        services.AddScoped<InterswitchTransactionService>();
        services.AddScoped<IInterswitchGovernmentCollectionsService, InterswitchGovernmentCollectionsService>();

        return services;
    }
}