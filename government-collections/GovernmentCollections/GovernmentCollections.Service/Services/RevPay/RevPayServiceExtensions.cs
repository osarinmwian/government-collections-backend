using GovernmentCollections.Service.Services.RevPay.BillType;
using GovernmentCollections.Service.Services.RevPay.Payment;
using GovernmentCollections.Service.Services.RevPay.Transaction;
using GovernmentCollections.Service.Services.RevPay.Validation;
using GovernmentCollections.Service.Services.Settlement;
using Microsoft.Extensions.DependencyInjection;

namespace GovernmentCollections.Service.Services.RevPay;

public static class RevPayServiceExtensions
{
    public static IServiceCollection AddRevPayServices(this IServiceCollection services)
    {
        services.AddScoped<RevPay.Validation.IPinValidationService, RevPay.Validation.PinValidationService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IRevPayBillTypeService, RevPayBillTypeService>();
        services.AddScoped<IRevPayPaymentService, RevPayPaymentService>();
        services.AddScoped<IRevPayTransactionService, RevPayTransactionService>();
        services.AddScoped<IRevPayService, RevPayService>();
        
        return services;
    }
}