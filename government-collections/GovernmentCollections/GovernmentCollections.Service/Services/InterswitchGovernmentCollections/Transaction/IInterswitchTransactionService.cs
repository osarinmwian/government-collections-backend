using GovernmentCollections.Domain.DTOs.Interswitch;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Transaction;

public interface IInterswitchTransactionService
{
    Task<InterswitchPaymentResponse> ProcessTransactionAsync(InterswitchTransactionRequest request);
    Task<InterswitchPaymentResponse> GetTransactionStatusAsync(string requestReference);
    Task<InterswitchTransactionHistoryResponse> GetTransactionHistoryAsync(string userId, int page, int pageSize);
    Task<InterswitchCustomerValidationResponse> ValidateCustomersAsync(InterswitchCustomerValidationBatchRequest request);
}