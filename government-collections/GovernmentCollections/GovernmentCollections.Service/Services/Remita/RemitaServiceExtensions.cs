using GovernmentCollections.Service.Services.Remita.Authentication;
using GovernmentCollections.Service.Services.Remita.BillPayment;
using GovernmentCollections.Service.Services.Remita.Payment;
using GovernmentCollections.Service.Services.Remita.Transaction;
using GovernmentCollections.Service.Services.Remita.Invoice;
using GovernmentCollections.Service.Services.Remita.Gateway;
using GovernmentCollections.Service.Services.Remita.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GovernmentCollections.Service.Services.Remita;

public static class RemitaServiceExtensions
{
    public static IServiceCollection AddRemitaServices(this IServiceCollection services)
    {
        services.AddScoped<IRemitaAuthenticationService, RemitaAuthenticationService>();
        services.AddScoped<IRemitaBillPaymentService, RemitaBillPaymentService>();
        services.AddScoped<IRemitaPaymentService, RemitaPaymentService>();
        services.AddScoped<IRemitaTransactionService, RemitaTransactionService>();
        services.AddScoped<IRemitaInvoiceService, RemitaInvoiceService>();
        services.AddScoped<IRemitaPaymentGatewayService, RemitaPaymentGatewayService>();
        services.AddScoped<IPinValidationService, PinValidationService>();
        services.AddScoped<IRemitaService, RemitaService>();
        
        return services;
    }
}