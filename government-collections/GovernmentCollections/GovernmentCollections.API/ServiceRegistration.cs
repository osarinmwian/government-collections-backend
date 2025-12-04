using GovernmentCollections.Service.Services.Remita;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections;
using GovernmentCollections.Service.Services.BuyPower;
using GovernmentCollections.Service.Services.RevPay;
using GovernmentCollections.Domain.Settings;
using System.Text;

namespace GovernmentCollections.API;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentGatewayServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure settings
        services.Configure<InterswitchSettings>(configuration.GetSection("InterswitchSettings"));
        services.Configure<RemitaSettings>(configuration.GetSection("RemitaSettings"));
        services.Configure<BuyPowerSettings>(configuration.GetSection("BuyPowerSettings"));
        services.Configure<RevPaySettings>(configuration.GetSection("RevPaySettings"));

        // Add memory cache for token caching
        services.AddMemoryCache();

        // Register Interswitch services
        services.AddHttpClient<IInterswitchGovernmentCollectionsService, InterswitchGovernmentCollectionsService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "KeyMobile-GovernmentCollections/1.0");
        });
        services.AddScoped<IInterswitchGovernmentCollectionsService, InterswitchGovernmentCollectionsService>();
        
        // Register Interswitch dependency services
        services.AddHttpClient<InterswitchAuthService>();
        services.AddScoped<InterswitchAuthService>();
        
        services.AddHttpClient<InterswitchServicesService>();
        services.AddScoped<InterswitchServicesService>();
        
        services.AddHttpClient<InterswitchTransactionService>();
        services.AddScoped<InterswitchTransactionService>();

        // Register Remita services
        services.AddRemitaServices(configuration);

        // Register other payment services
        services.AddScoped<IBuyPowerService, BuyPowerService>();
        services.AddScoped<IRevPayService, RevPayService>();

        return services;
    }

    public static IServiceCollection AddRemitaServices(this IServiceCollection services, IConfiguration configuration)
    {
        var username = configuration["Remita:Username"];
        var password = configuration["Remita:Password"];
        
        // Register HttpClient for RemitaService with authentication
        services.AddHttpClient<IRemitaService, RemitaService>(client =>
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // Register HttpClient for Remita API
        services.AddHttpClient<IRemitaAuthService, RemitaAuthService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("Content-Type", "application/json");
        });

        services.AddHttpClient<IRemitaInvoiceService, RemitaInvoiceService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"]!);
        });

        services.AddHttpClient<IRemitaPaymentGatewayService, RemitaPaymentGatewayService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"]!);
        });

        // Register services
        services.AddScoped<IRemitaAuthService, RemitaAuthService>();
        services.AddScoped<IRemitaInvoiceService, RemitaInvoiceService>();
        services.AddScoped<IRemitaPaymentGatewayService, RemitaPaymentGatewayService>();
        services.AddScoped<IPinValidationService, PinValidationService>();

        return services;
    }
}