using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;
using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Domain.DTOs.Settlement;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Services.Settlement;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.BillPayment;

public class InterswitchBillPaymentService : IInterswitchBillPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InterswitchBillPaymentService> _logger;
    private readonly IMemoryCache _cache;
    private readonly InterswitchSettings _settings;
    private readonly InterswitchAuthService _authService;
    private readonly ISettlementService _settlementService;
    private const string BILLERS_CACHE_KEY = "interswitch_government_billers";

    public InterswitchBillPaymentService(
        HttpClient httpClient,
        ILogger<InterswitchBillPaymentService> logger,
        IMemoryCache cache,
        IOptions<InterswitchSettings> settings,
        InterswitchAuthService authService,
        ISettlementService settlementService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _settings = settings.Value;
        _authService = authService;
        _settlementService = settlementService;
    }

    public async Task<List<InterswitchBiller>> GetGovernmentBillersAsync()
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            if (_cache.TryGetValue(BILLERS_CACHE_KEY, out List<InterswitchBiller>? cachedBillers) && cachedBillers != null)
            {
                return cachedBillers;
            }

            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/services";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetGovernmentBillersAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetGovernmentBillersAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[ERROR-{RequestId}] Failed to get billers. Status: {StatusCode}", requestId, response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var servicesResponse = JsonSerializer.Deserialize<InterswitchServicesResponse>(responseContent);

            if (servicesResponse?.BillerList?.Categories == null)
            {
                return new List<InterswitchBiller>();
            }

            var governmentKeywords = new[] { 
                "Tax", "Revenue", "Government", "State", "Federal", "Ministry", "Agency",
                "FIRS", "IRS", "Custom", "FRSC", "Road Safety", "Immigration", 
                "Police", "Court", "Judiciary", "Council", "Authority", "Commission",
                "Treasury", "Finance", "Budget", "Levy", "Fee", "Permit", "License"
            };
            
            var governmentBillers = new List<InterswitchBiller>();

            foreach (var category in servicesResponse.BillerList.Categories)
            {
                // Include billers that match government keywords
                var govBillers = category.Billers.Where(b => 
                    governmentKeywords.Any(keyword => 
                        b.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        b.Narration.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        b.ShortName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                    ) ||
                    // Include specific government category names
                    category.Name.Equals("State Payments", StringComparison.OrdinalIgnoreCase) ||
                    category.Name.Equals("Tax Payments", StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                governmentBillers.AddRange(govBillers);
            }

            _cache.Set(BILLERS_CACHE_KEY, governmentBillers, TimeSpan.FromHours(1));
            
            _logger.LogInformation("[SUCCESS-{RequestId}] Retrieved {Count} government billers", requestId, governmentBillers.Count);
            return governmentBillers;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetGovernmentBillersAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<List<InterswitchBiller>> GetBillersByCategoryAsync(int categoryId)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/services?categoryId={categoryId}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetBillersByCategoryAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetBillersByCategoryAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var servicesResponse = JsonSerializer.Deserialize<InterswitchServicesResponse>(responseContent);
            return servicesResponse?.BillerList?.Categories?.FirstOrDefault()?.Billers ?? new List<InterswitchBiller>();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetBillersByCategoryAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<List<InterswitchCategory>> GetGovernmentCategoriesAsync()
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/services/categories";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetGovernmentCategoriesAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetGovernmentCategoriesAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var servicesResponse = JsonSerializer.Deserialize<InterswitchServicesResponse>(responseContent);

            if (servicesResponse?.BillerList?.Categories == null)
            {
                return new List<InterswitchCategory>();
            }

            var strictGovernmentCategories = new[] { "State Payments", "Tax Payments" };
            var governmentCategories = servicesResponse.BillerList.Categories
                .Where(c => strictGovernmentCategories.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInformation("[SUCCESS-{RequestId}] Retrieved {Count} government categories", requestId, governmentCategories.Count);
            return governmentCategories;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetGovernmentCategoriesAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<List<InterswitchPaymentItem>> GetServiceOptionsAsync(int serviceId)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var authResponse = await _authService.AuthenticateAsync();
            var terminalId = authResponse.TerminalId;
            
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/services/options?serviceid={serviceId}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetServiceOptionsAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetServiceOptionsAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var paymentItems = JsonSerializer.Deserialize<List<InterswitchPaymentItem>>(responseContent) ?? new List<InterswitchPaymentItem>();
            
            _logger.LogInformation("[SUCCESS-{RequestId}] Retrieved {Count} payment items", requestId, paymentItems.Count);
            return paymentItems;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetServiceOptionsAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<InterswitchPaymentResponse> ProcessTransactionAsync(InterswitchTransactionRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var authResponse = await _authService.AuthenticateAsync();
            var terminalId = authResponse.TerminalId;
            
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions";
            
            var transactionData = new
            {
                TerminalId = terminalId,
                paymentCode = request.PaymentCode,
                customerId = request.CustomerId,
                customerMobile = request.CustomerMobile,
                customerEmail = request.CustomerEmail,
                amount = request.Amount.ToString(),
                requestReference = request.RequestReference
            };
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            var jsonContent = JsonSerializer.Serialize(transactionData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("[OUTBOUND-{RequestId}] ProcessTransactionAsync: POST {Url}", requestId, requestUrl);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ProcessTransactionAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var paymentResponse = JsonSerializer.Deserialize<InterswitchPaymentResponse>(responseContent) ?? new InterswitchPaymentResponse();
            
            // Process settlement if transaction was successful
            if (paymentResponse.ResponseCode == "00")
            {
                var settlementResult = await _settlementService.ProcessSettlementAsync(
                    request.RequestReference,
                    request.CustomerId,
                    request.Amount,
                    $"Interswitch Payment - {request.PaymentCode}",
                    "INTERSWITCH");
                _logger.LogInformation("[SETTLEMENT-{RequestId}] Settlement result: {Status} - {Message}", requestId, settlementResult.ResponseStatus, settlementResult.ResponseMessage);
                
                // Add settlement reference to response if successful
                if (settlementResult.ResponseStatus)
                {
                    paymentResponse.SettlementReference = settlementResult.ResponseData;
                }
            }
            
            _logger.LogInformation("[SUCCESS-{RequestId}] Transaction processed: {TransactionRef}", requestId, paymentResponse.TransactionReference);
            return paymentResponse;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ProcessTransactionAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<InterswitchPaymentResponse> GetTransactionStatusAsync(string requestReference)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var authResponse = await _authService.AuthenticateAsync();
            var terminalId = authResponse.TerminalId;
            
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions?requestRef={requestReference}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetTransactionStatusAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetTransactionStatusAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var paymentResponse = JsonSerializer.Deserialize<InterswitchPaymentResponse>(responseContent) ?? new InterswitchPaymentResponse();
            
            _logger.LogInformation("[SUCCESS-{RequestId}] Transaction status retrieved: {Status}", requestId, paymentResponse.PaymentStatus);
            return paymentResponse;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetTransactionStatusAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<InterswitchCustomerValidationResponse> ValidateCustomersAsync(InterswitchCustomerValidationBatchRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var authResponse = await _authService.AuthenticateAsync();
            var terminalId = authResponse.TerminalId;
            
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions/validatecustomers";
            
            var validationData = new
            {
                customers = request.Customers.Select(c => new
                {
                    PaymentCode = c.PaymentCode,
                    CustomerId = c.CustomerId
                }).ToList(),
                TerminalId = terminalId
            };
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            var jsonContent = JsonSerializer.Serialize(validationData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("[OUTBOUND-{RequestId}] ValidateCustomersAsync: POST {Url}", requestId, requestUrl);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ValidateCustomersAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var validationResponse = JsonSerializer.Deserialize<InterswitchCustomerValidationResponse>(responseContent) ?? new InterswitchCustomerValidationResponse();
            
            _logger.LogInformation("[SUCCESS-{RequestId}] Customer validation completed", requestId);
            return validationResponse;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ValidateCustomersAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }
}