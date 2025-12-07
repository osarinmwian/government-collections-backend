using GovernmentCollections.Service.Services.Remita;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections;
using GovernmentCollections.Service.Services.BuyPower;
using GovernmentCollections.Service.Services.RevPay;
using GovernmentCollections.Service.Services.Settlement;
using GovernmentCollections.Shared.Validation;

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
        services.Configure<FundTransferApiUrl>(configuration.GetSection("FundTransferApiUrl"));
        services.Configure<SettlementAccountSettings>(configuration.GetSection("SettlementAccountSettings"));
        services.Configure<EncryptionSettings>(configuration.GetSection("EncryptionSettings"));

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
        
        services.AddHttpClient<InterswitchTransactionService>();
        services.AddScoped<InterswitchTransactionService>();
        
        services.AddHttpClient<GovernmentCollections.Service.Services.InterswitchGovernmentCollections.BillPayment.InterswitchBillPaymentService>();
        services.AddScoped<GovernmentCollections.Service.Services.InterswitchGovernmentCollections.BillPayment.InterswitchBillPaymentService>();
        


        // Register Remita services
        services.AddRemitaServices(configuration);

        // Register other payment services
        services.AddScoped<IBuyPowerService, BuyPowerService>();
        services.AddScoped<IRevPayService, RevPayService>();
        
        // Register shared validation service
        services.AddScoped<IPinValidationService, PinValidationService>();
        
        // Register settlement service
        services.AddHttpClient<ISettlementService, SettlementService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddScoped<ISettlementService, SettlementService>();

        return services;
    }

    public static IServiceCollection AddRemitaServices(this IServiceCollection services, IConfiguration configuration)
    {
        var username = configuration["Remita:Username"];
        var password = configuration["Remita:Password"];
        


        // Register HttpClients for Remita services
        services.AddHttpClient<GovernmentCollections.Service.Services.Remita.Authentication.RemitaAuthenticationService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"] ?? "https://api.remita.net");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<GovernmentCollections.Service.Services.Remita.BillPayment.RemitaBillPaymentService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"] ?? "https://api.remita.net");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<GovernmentCollections.Service.Services.Remita.Payment.RemitaPaymentService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"] ?? "https://api.remita.net");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        services.AddHttpClient<GovernmentCollections.Service.Services.Remita.Transaction.RemitaTransactionService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Remita:BaseUrl"] ?? "https://api.remita.net");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Remita services
        services.AddScoped<GovernmentCollections.Service.Services.Remita.Authentication.IRemitaAuthenticationService, GovernmentCollections.Service.Services.Remita.Authentication.RemitaAuthenticationService>();
        services.AddScoped<GovernmentCollections.Service.Services.Remita.BillPayment.IRemitaBillPaymentService, GovernmentCollections.Service.Services.Remita.BillPayment.RemitaBillPaymentService>();
        services.AddScoped<GovernmentCollections.Service.Services.Remita.Payment.IRemitaPaymentService, GovernmentCollections.Service.Services.Remita.Payment.RemitaPaymentService>();
        services.AddScoped<GovernmentCollections.Service.Services.Remita.Transaction.IRemitaTransactionService, GovernmentCollections.Service.Services.Remita.Transaction.RemitaTransactionService>();

        services.AddScoped<GovernmentCollections.Service.Services.Settlement.ISettlementService, GovernmentCollections.Service.Services.Settlement.SettlementService>();
        
        // Register main RemitaService
        services.AddScoped<IRemitaService, RemitaService>();

        return services;
    }
}