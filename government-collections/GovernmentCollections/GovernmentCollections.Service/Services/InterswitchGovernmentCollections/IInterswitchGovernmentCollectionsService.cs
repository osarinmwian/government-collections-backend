using GovernmentCollections.Domain.DTOs.Interswitch;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public interface IInterswitchGovernmentCollectionsService
{
    Task<InterswitchAuthResponse> AuthenticateAsync();
    Task<List<InterswitchBiller>> GetGovernmentBillersAsync();
    Task<List<InterswitchBiller>> GetBillersByCategoryAsync(int categoryId);
    Task<List<InterswitchCategory>> GetGovernmentCategoriesAsync();
    Task<List<InterswitchPaymentItem>> GetServiceOptionsAsync(int serviceId);
    Task<List<InterswitchPaymentItem>> GetPaymentItemsAsync(int billerId, string customerReference);
    Task<InterswitchBillInquiryResponse> BillInquiryAsync(InterswitchBillInquiryRequest request);
    Task<InterswitchCustomerValidationResponse> ValidateCustomersAsync(InterswitchCustomerValidationBatchRequest request);
    Task<InterswitchPaymentResponse> ProcessPaymentAsync(InterswitchPaymentRequest request);
    Task<InterswitchPaymentResponse> ProcessTransactionAsync(InterswitchTransactionRequest request);
    Task<InterswitchPaymentResponse> GetTransactionStatusAsync(string requestReference);
    Task<InterswitchPaymentResponse> VerifyTransactionAsync(string requestReference);
    Task<InterswitchTransactionHistoryResponse> GetTransactionHistoryAsync(string userId, int page, int pageSize);
    Task<bool> IsTokenValidAsync();
}