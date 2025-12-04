using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Domain.Settings;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public class InterswitchGovernmentCollectionsService : IInterswitchGovernmentCollectionsService
{
    private readonly InterswitchAuthService _authService;
    private readonly InterswitchServicesService _servicesService;
    private readonly InterswitchTransactionService _transactionService;
    private readonly ILogger<InterswitchGovernmentCollectionsService> _logger;

    public InterswitchGovernmentCollectionsService(
        InterswitchAuthService authService,
        InterswitchServicesService servicesService,
        InterswitchTransactionService transactionService,
        ILogger<InterswitchGovernmentCollectionsService> logger)
    {
        _authService = authService;
        _servicesService = servicesService;
        _transactionService = transactionService;
        _logger = logger;
    }

    public async Task<InterswitchAuthResponse> AuthenticateAsync()
    {
        return await _authService.AuthenticateAsync();
    }

    public async Task<bool> IsTokenValidAsync()
    {
        return await _authService.IsTokenValidAsync();
    }

    public async Task<List<InterswitchBiller>> GetGovernmentBillersAsync()
    {
        return await _servicesService.GetGovernmentBillersAsync();
    }

    public async Task<List<InterswitchBiller>> GetBillersByCategoryAsync(int categoryId)
    {
        return await _servicesService.GetBillersByCategoryAsync(categoryId);
    }

    public async Task<List<InterswitchCategory>> GetGovernmentCategoriesAsync()
    {
        return await _servicesService.GetGovernmentCategoriesAsync();
    }

    public async Task<List<InterswitchPaymentItem>> GetServiceOptionsAsync(int serviceId)
    {
        return await _servicesService.GetServiceOptionsAsync(serviceId);
    }

    public async Task<List<InterswitchPaymentItem>> GetPaymentItemsAsync(int billerId, string customerReference)
    {
        return await _servicesService.GetServiceOptionsAsync(billerId);
    }

    public async Task<InterswitchBillInquiryResponse> BillInquiryAsync(InterswitchBillInquiryRequest request)
    {
        return await _transactionService.BillInquiryAsync(request);
    }



    public async Task<InterswitchPaymentResponse> ProcessPaymentAsync(InterswitchPaymentRequest request)
    {
        return await _transactionService.ProcessPaymentAsync(request);
    }

    public async Task<InterswitchPaymentResponse> VerifyTransactionAsync(string requestReference)
    {
        return await _transactionService.VerifyTransactionAsync(requestReference);
    }

    public async Task<InterswitchTransactionHistoryResponse> GetTransactionHistoryAsync(string userId, int page, int pageSize)
    {
        return await _transactionService.GetTransactionHistoryAsync(userId, page, pageSize);
    }

    public async Task<InterswitchCustomerValidationResponse> ValidateCustomersAsync(InterswitchCustomerValidationBatchRequest request)
    {
        return await _servicesService.ValidateCustomersAsync(request);
    }

    public async Task<InterswitchPaymentResponse> ProcessTransactionAsync(InterswitchTransactionRequest request)
    {
        return await _servicesService.ProcessTransactionAsync(request);
    }

    public async Task<InterswitchPaymentResponse> GetTransactionStatusAsync(string requestReference)
    {
        return await _servicesService.GetTransactionStatusAsync(requestReference);
    }
}