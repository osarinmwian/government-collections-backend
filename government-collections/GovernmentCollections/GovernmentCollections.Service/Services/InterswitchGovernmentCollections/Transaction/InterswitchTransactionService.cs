using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Domain.Settings;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public class InterswitchTransactionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InterswitchTransactionService> _logger;
    private readonly InterswitchSettings _settings;
    private readonly InterswitchAuthService _authService;

    public InterswitchTransactionService(
        HttpClient httpClient,
        ILogger<InterswitchTransactionService> logger,
        IOptions<InterswitchSettings> settings,
        InterswitchAuthService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
        _authService = authService;
    }

    public async Task<InterswitchBillInquiryResponse> BillInquiryAsync(InterswitchBillInquiryRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/billinquiry";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[OUTBOUND-{RequestId}] BillInquiryAsync: POST {Url}", requestId, requestUrl);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] BillInquiryAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var inquiryResponse = JsonSerializer.Deserialize<InterswitchBillInquiryResponse>(responseContent);

            _logger.LogInformation("[SUCCESS-{RequestId}] Bill inquiry successful for biller {BillerId}", requestId, request.BillerId);
            return inquiryResponse ?? new InterswitchBillInquiryResponse { ResponseCode = "99", ResponseMessage = "Invalid response" };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] BillInquiryAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<InterswitchPaymentResponse> ProcessPaymentAsync(InterswitchPaymentRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            request.TerminalId = terminalId;
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[OUTBOUND-{RequestId}] ProcessPaymentAsync: POST {Url}", requestId, requestUrl);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ProcessPaymentAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var paymentResponse = JsonSerializer.Deserialize<InterswitchPaymentResponse>(responseContent);

            _logger.LogInformation("[SUCCESS-{RequestId}] Payment processed for reference {Reference}", requestId, request.RequestReference);
            return paymentResponse ?? new InterswitchPaymentResponse { ResponseCode = "99", ResponseDescription = "Invalid response" };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ProcessPaymentAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
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
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions/validatecustomers";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            request.TerminalId = terminalId;
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("[OUTBOUND-{RequestId}] ValidateCustomersAsync: POST {Url}", requestId, requestUrl);
            
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ValidateCustomersAsync: Status={StatusCode} | Duration={Duration}ms | Response={Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                return new InterswitchCustomerValidationResponse 
                { 
                    ResponseCode = ((int)response.StatusCode).ToString(), 
                    ResponseCodeGrouping = "FAILED" 
                };
            }

            var validationResponse = JsonSerializer.Deserialize<InterswitchCustomerValidationResponse>(responseContent);

            _logger.LogInformation("[SUCCESS-{RequestId}] Customer validation successful", requestId);
            return validationResponse ?? new InterswitchCustomerValidationResponse { ResponseCode = "99", ResponseCodeGrouping = "FAILED" };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ValidateCustomersAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            return new InterswitchCustomerValidationResponse 
            { 
                ResponseCode = "99", 
                ResponseCodeGrouping = "FAILED" 
            };
        }
    }

    public async Task<InterswitchPaymentResponse> VerifyTransactionAsync(string requestReference)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/Transactions?requestRef={requestReference}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] VerifyTransactionAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] VerifyTransactionAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var verificationResponse = JsonSerializer.Deserialize<InterswitchPaymentResponse>(responseContent);

            _logger.LogInformation("[SUCCESS-{RequestId}] Transaction verification successful", requestId);
            return verificationResponse ?? new InterswitchPaymentResponse { ResponseCode = "99", ResponseDescription = "Invalid response" };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] VerifyTransactionAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<InterswitchTransactionHistoryResponse> GetTransactionHistoryAsync(string userId, int page, int pageSize)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var token = await _authService.GetValidTokenAsync();
            var terminalId = await _authService.GetTerminalIdAsync();
            var requestUrl = $"{_settings.ServicesUrl}/quicktellerservice/api/v5/transactions?userId={userId}&page={page}&pageSize={pageSize}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("TerminalID", terminalId);

            _logger.LogInformation("[OUTBOUND-{RequestId}] GetTransactionHistoryAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetTransactionHistoryAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            response.EnsureSuccessStatusCode();

            var historyResponse = JsonSerializer.Deserialize<InterswitchTransactionHistoryResponse>(responseContent);

            _logger.LogInformation("[SUCCESS-{RequestId}] Retrieved transaction history for user {UserId}", requestId, userId);
            return historyResponse ?? new InterswitchTransactionHistoryResponse();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetTransactionHistoryAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }
}