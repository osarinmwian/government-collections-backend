using FluentValidation;
using GovernmentCollections.Data.Context;
using GovernmentCollections.Data.Repositories;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Domain.Validators;
using GovernmentCollections.Service.Gateways;
using GovernmentCollections.Service.Services;
using GovernmentCollections.Service.Services.Remita;
using GovernmentCollections.Service.Services.BuyPower;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections;
using GovernmentCollections.Service.Services.RevPay;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GovernmentCollections.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // SQL Server Configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddScoped<IGovernmentCollectionsContext>(provider => 
            new GovernmentCollectionsContext(connectionString!));

        // Payment Gateway Settings (optional)
        var revPaySettings = configuration.GetSection("RevPaySettings").Get<RevPaySettings>();
        var remitaSettings = configuration.GetSection("RemitaSettings").Get<RemitaSettings>();
        var interswitchSettings = configuration.GetSection("InterswitchSettings").Get<InterswitchSettings>();
        var buyPowerSettings = configuration.GetSection("BuyPowerSettings").Get<BuyPowerSettings>();

        if (revPaySettings != null) services.AddSingleton(revPaySettings);
        if (remitaSettings != null) services.AddSingleton(remitaSettings);
        if (interswitchSettings != null) services.AddSingleton(interswitchSettings);
        if (buyPowerSettings != null) services.AddSingleton(buyPowerSettings);

        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Services
        services.AddScoped<IBuyPowerService, BuyPowerService>();
        services.AddScoped<IInterswitchGovernmentCollectionsService, InterswitchGovernmentCollectionsService>();
        services.AddRevPayServices();
        services.AddScoped<IRemitaService, RemitaService>();

        // Gateways
        services.AddHttpClient();
        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<PaymentRequestValidator>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
            };
        });

        return services;
    }
}