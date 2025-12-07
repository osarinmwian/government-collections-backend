using GovernmentCollections.Domain.DTOs.RevPay;

namespace GovernmentCollections.Service.Services.RevPay.Payment;

public interface IRevPayPaymentService
{
    Task<dynamic> ProcessPaymentAsync(RevPayPaymentRequest request);
    Task<dynamic> GenerateWebGuidAsync(RevPayWebGuidRequest request);
    Task<dynamic> GetReceiptAsync(RevPayReceiptRequest request);
}