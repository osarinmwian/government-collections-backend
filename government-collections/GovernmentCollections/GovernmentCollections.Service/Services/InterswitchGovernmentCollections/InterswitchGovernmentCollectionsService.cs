using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Authentication;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Transaction;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections.BillPayment;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Validation;
using GovernmentCollections.Service.Services.Settlement;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public class InterswitchGovernmentCollectionsService : IInterswitchGovernmentCollectionsService
{
    private readonly InterswitchAuthService _authService;
    private readonly InterswitchTransactionService _transactionService;
    private readonly InterswitchBillPaymentService _billPaymentService;
    private readonly IPinValidationService _pinValidationService;
    private readonly ISettlementService _settlementService;
    private readonly ILogger<InterswitchGovernmentCollectionsService> _logger;

    public InterswitchGovernmentCollectionsService(
        InterswitchAuthService authService,
        InterswitchTransactionService transactionService,
        InterswitchBillPaymentService billPaymentService,
        IPinValidationService pinValidationService,
        ISettlementService settlementService,
        ILogger<InterswitchGovernmentCollectionsService> logger)
    {
        _authService = authService;
        _transactionService = transactionService;
        _billPaymentService = billPaymentService;
        _pinValidationService = pinValidationService;
        _settlementService = settlementService;
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
        return await _billPaymentService.GetGovernmentBillersAsync();
    }

    public async Task<List<InterswitchBiller>> GetBillersByCategoryAsync(int categoryId)
    {
        return await _billPaymentService.GetBillersByCategoryAsync(categoryId);
    }

    public async Task<List<InterswitchCategory>> GetGovernmentCategoriesAsync()
    {
        return await _billPaymentService.GetGovernmentCategoriesAsync();
    }

    public async Task<List<InterswitchPaymentItem>> GetServiceOptionsAsync(int serviceId)
    {
        return await _billPaymentService.GetServiceOptionsAsync(serviceId);
    }

    public async Task<List<InterswitchPaymentItem>> GetPaymentItemsAsync(int billerId, string customerReference)
    {
        return new List<InterswitchPaymentItem>();
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
        return new InterswitchCustomerValidationResponse();
    }

    public async Task<InterswitchPaymentResponse> ProcessTransactionAsync(InterswitchTransactionRequest request)
    {
        try
        {
            // Validate PIN
            var pinValid = await _pinValidationService.ValidatePinAsync(request.CustomerId, request.Pin);
            if (!pinValid)
            {
                return new InterswitchPaymentResponse { ResponseCode = "01", ResponseMessage = "Invalid PIN" };
            }

            // Validate 2FA if required
            if (request.Enforce2FA)
            {
                var twoFaValid = await _pinValidationService.Validate2FAAsync(request.CustomerId, request.SecondFa, request.SecondFaType);
                if (!twoFaValid)
                {
                    return new InterswitchPaymentResponse { ResponseCode = "01", ResponseMessage = "Invalid 2FA" };
                }
            }

            // Convert to payment request format
            var paymentRequest = new InterswitchPaymentRequest
            {
                RequestReference = request.RequestReference,
                CustomerReference = request.CustomerId,
                CustomerPhone = request.CustomerMobile,
                CustomerEmail = request.CustomerEmail,
                Amount = request.Amount
            };
            
            var response = await _transactionService.ProcessPaymentAsync(paymentRequest);
            
            // Process settlement if payment successful
            if (response.ResponseCode == "00")
            {
                await _settlementService.ProcessSettlementAsync(new Domain.DTOs.Settlement.SettlementRequest
                {
                    TransactionReference = request.RequestReference,
                    Amount = request.Amount,
                    AccountNumber = request.CustomerId,
                    Channel = request.Channel,
                    PaymentGateway = "Interswitch"
                });
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Interswitch transaction");
            return new InterswitchPaymentResponse { ResponseCode = "99", ResponseMessage = "Transaction processing failed" };
        }
    }

    public async Task<InterswitchPaymentResponse> GetTransactionStatusAsync(string requestReference)
    {
        return await _transactionService.VerifyTransactionAsync(requestReference);
    }
}