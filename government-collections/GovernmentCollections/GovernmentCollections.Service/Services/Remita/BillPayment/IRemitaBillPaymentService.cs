using GovernmentCollections.Domain.DTOs.Remita;

namespace GovernmentCollections.Service.Services.Remita.BillPayment;

public interface IRemitaBillPaymentService
{
    Task<List<RemitaBillerDto>> GetBillersAsync();
    Task<RemitaBillerDetailsDto> GetBillerByIdAsync(string billerId);
    Task<RemitaValidateCustomerResponse> ValidateCustomerAsync(RemitaValidateCustomerRequest request);
}