using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Validation;
using GovernmentCollections.Service.Services.Settlement;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public static class InterswitchServiceExtensions
{
    public static IServiceCollection AddInterswitchServices(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("InterswitchSettings").Get<InterswitchSettings>() ?? new InterswitchSettings();
        services.AddSingleton(settings);

        services.AddHttpClient<InterswitchAuthService>();
        services.AddHttpClient<InterswitchTransactionService>();

        services.AddScoped<InterswitchGovernmentCollections.Validation.IPinValidationService, InterswitchGovernmentCollections.Validation.PinValidationService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<InterswitchAuthService>();
        services.AddScoped<InterswitchTransactionService>();
        services.AddScoped<IInterswitchGovernmentCollectionsService, InterswitchGovernmentCollectionsService>();

        return services;
    }
}