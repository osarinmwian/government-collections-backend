using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.DTOs.Remita;

namespace GovernmentCollections.Service.Services.Remita;

public interface IRemitaService
{
    Task<List<RemitaBillerDto>> GetBillersAsync();
    Task<RemitaBillerDetailsDto> GetBillerByIdAsync(string billerId);
    Task<RemitaValidateCustomerResponse> ValidateCustomerAsync(RemitaValidateCustomerRequest request);
    Task<RemitaPaymentResponse> ProcessPaymentAsync(RemitaPaymentRequest request);
    Task<dynamic> ProcessTransactionWithAuthAsync(RemitaTransactionInitiateDto request);
    Task<dynamic> InitiatePaymentAsync(RemitaInitiatePaymentDto request);
    Task<dynamic> VerifyPaymentAsync(string rrr);
    Task<dynamic> GetTransactionStatusAsync(string transactionId);
    Task<dynamic> GetActiveBanksAsync();
    Task<dynamic> ActivateMandateAsync(RemitaRrrPaymentRequest request);
    Task<dynamic> GetRrrDetailsAsync(string rrr);
    Task<dynamic> ActivateRrrPaymentAsync(RemitaRrrPaymentRequest request);
    Task<dynamic> ProcessRrrPaymentAsync(RemitaRrrPaymentRequest request);
}