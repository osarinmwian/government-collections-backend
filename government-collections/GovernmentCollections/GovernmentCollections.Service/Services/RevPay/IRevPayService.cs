using GovernmentCollections.Domain.DTOs.RevPay;

namespace GovernmentCollections.Service.Services.RevPay;

public interface IRevPayService
{
    Task<dynamic> GetBillTypesAsync();
    Task<dynamic> ValidateReferenceAsync(RevPayValidateRequest request);
    Task<dynamic> ProcessPaymentAsync(RevPayPaymentRequest request);
    Task<dynamic> GenerateWebGuidAsync(RevPayWebGuidRequest request);
    Task<dynamic> VerifyPidAsync(RevPayPidVerificationRequest request);
    Task<dynamic> GetReceiptAsync(RevPayReceiptRequest request);
    Task<dynamic> ProcessTransactionWithAuthAsync(RevPayTransactionRequest request);
}