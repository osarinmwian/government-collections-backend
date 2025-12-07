using GovernmentCollections.Domain.DTOs.Remita;

namespace GovernmentCollections.Service.Services.Remita.Payment;

public interface IRemitaPaymentService
{
    Task<RemitaPaymentResponse> ProcessPaymentAsync(RemitaPaymentRequest request);
    Task<dynamic> InitiatePaymentAsync(RemitaInitiatePaymentDto request);
    Task<dynamic> VerifyPaymentAsync(string rrr);
    Task<dynamic> GetActiveBanksAsync();
    Task<dynamic> ActivateMandateAsync(RemitaRrrPaymentRequest request);
    Task<dynamic> GetRrrDetailsAsync(string rrr);
    Task<dynamic> ActivateRrrPaymentAsync(RemitaRrrPaymentRequest request);
    Task<dynamic> ProcessRrrPaymentAsync(RemitaRrrPaymentRequest request);
}