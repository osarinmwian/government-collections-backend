// Add this to your OmniChannel Program.cs or Startup.cs

using KeyLoyalty.Infrastructure.Services;
using KeyLoyalty.Infrastructure.Repositories;

public class OmniChannelStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Your existing service registrations...

        // ADD THESE LOYALTY SERVICES
        services.AddScoped<ILoyaltyTransactionTracker, LoyaltyTransactionTracker>();
        services.AddScoped<ICustomerLoyaltyRepository, CustomerLoyaltyRepository>();
        services.AddScoped<IAccountMappingService, AccountMappingService>();
        
        // Configure HttpClient for notifications
        services.AddHttpClient<ILoyaltyTransactionTracker>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["NotificationService:BaseUrl"] ?? "http://localhost:5000";
            var apiKey = configuration["NotificationService:ApiKey"];
            
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        });

        // Your existing services continue...
    }
}

// Alternative: If using minimal API in Program.cs
public class ProgramSetup
{
    public static void AddLoyaltyServices(WebApplicationBuilder builder)
    {
        // Add to your existing Program.cs
        builder.Services.AddScoped<ILoyaltyTransactionTracker, LoyaltyTransactionTracker>();
        builder.Services.AddScoped<ICustomerLoyaltyRepository, CustomerLoyaltyRepository>();
        builder.Services.AddScoped<IAccountMappingService, AccountMappingService>();
        
        builder.Services.AddHttpClient<ILoyaltyTransactionTracker>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["NotificationService:BaseUrl"] ?? ""));
        });
    }
}