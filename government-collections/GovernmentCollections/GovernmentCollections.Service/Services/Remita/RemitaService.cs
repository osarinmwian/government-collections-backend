using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.DTOs.Remita;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Remita;

public class RemitaService : IRemitaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaService> _logger;
    private readonly IPinValidationService _pinValidationService;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public RemitaService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaService> logger, IPinValidationService pinValidationService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _pinValidationService = pinValidationService;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        var username = _configuration["Remita:Username"];
        var password = _configuration["Remita:Password"];
        var baseUrl = _configuration["Remita:BaseUrl"];
        var tokenUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/uaasvc/uaa/token";

        var authRequest = new { username, password };
        var json = JsonSerializer.Serialize(authRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var authResponse = JsonSerializer.Deserialize<RemitaAuthResponse>(responseContent);
            if (authResponse?.Data?.Count > 0)
            {
                _accessToken = authResponse.Data[0].AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse.Data[0].ExpiresIn - 60);
                return _accessToken;
            }
        }
        throw new Exception("Failed to authenticate with Remita");
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<List<RemitaBillerDto>> GetBillersAsync()
    {
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/billers";
        
        var response = await _httpClient.GetAsync(requestUrl);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            var billersResponse = JsonSerializer.Deserialize<RemitaBillersResponse>(responseContent);
            return billersResponse?.Data?.Select(b => new RemitaBillerDto
            {
                BillerId = b.BillerId,
                Name = b.BillerName,
                Logo = b.BillerLogoUrl ?? "",
                Category = b.CategoryName
            }).ToList() ?? new List<RemitaBillerDto>();
        }
        return new List<RemitaBillerDto>();
    }

    public async Task<RemitaBillerDetailsDto> GetBillerByIdAsync(string billerId)
    {
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/{billerId}/products";
        
        var response = await _httpClient.GetAsync(requestUrl);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (response.IsSuccessStatusCode)
        {
            var productsResponse = JsonSerializer.Deserialize<RemitaBillerProductsResponse>(responseContent);
            return new RemitaBillerDetailsDto
            {
                Status = productsResponse?.Status ?? "00",
                Message = productsResponse?.Message ?? "Request processed successfully",
                Data = productsResponse?.Data ?? new RemitaBillerProductsData()
            };
        }
        return new RemitaBillerDetailsDto
        {
            Status = "01",
            Message = "Failed to retrieve biller products",
            Data = new RemitaBillerProductsData()
        };
    }

    public async Task<RemitaValidateCustomerResponse> ValidateCustomerAsync(RemitaValidateCustomerRequest request)
    {
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/customer/validation";
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await _httpClient.PostAsync(requestUrl, content, cts.Token);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var validationResponse = JsonSerializer.Deserialize<RemitaCustomerValidationResponse>(responseContent);
                return new RemitaValidateCustomerResponse
                {
                    Status = validationResponse?.Status ?? "00",
                    Message = validationResponse?.Message ?? "Request processed successfully",
                    Data = validationResponse?.Data
                };
            }
            return new RemitaValidateCustomerResponse 
            { 
                Status = "01", 
                Message = "Validation failed",
                Data = null
            };
        }
        catch (TaskCanceledException)
        {
            return new RemitaValidateCustomerResponse 
            { 
                Status = "99", 
                Message = "Request timeout",
                Data = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating customer");
            return new RemitaValidateCustomerResponse 
            { 
                Status = "01", 
                Message = "Validation failed",
                Data = null
            };
        }
    }

    public async Task<RemitaPaymentResponse> ProcessPaymentAsync(RemitaPaymentRequest request)
    {
        await SetAuthHeaderAsync();
        var transactionRef = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        
        // Step 1: Initialize transaction
        var initRequest = new RemitaTransactionInitiateDto
        {
            BillPaymentProductId = request.ProductId,
            Amount = request.Amount,
            TransactionRef = transactionRef,
            Name = request.Name,
            Email = request.Email,
            PhoneNumber = request.Phone,
            CustomerId = request.CustomerId
        };
        
        var baseUrl = _configuration["Remita:BaseUrl"];
        var initUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/initiate";
        
        var json = JsonSerializer.Serialize(initRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var initResponse = await _httpClient.PostAsync(initUrl, content);
        var initResponseContent = await initResponse.Content.ReadAsStringAsync();
        
        if (!initResponse.IsSuccessStatusCode)
        {
            return new RemitaPaymentResponse { Status = "ERROR", Rrr = "", Amount = 0, Paid = false, ReceiptUrl = "" };
        }
        
        var initResult = JsonSerializer.Deserialize<RemitaTransactionInitResponse>(initResponseContent);
        var rrr = request.TransactionRef; // Use transaction ref as RRR
        
        // Step 2: Process payment
        var payRequest = new RemitaPaymentProcessDto
        {
            Rrr = rrr,
            TransactionRef = transactionRef,
            Amount = request.Amount,
            Channel = "internetbanking",
            Metadata = new RemitaPaymentMetadata
            {
                FundingSource = "HERITAGE",
                PayerAccountNumber = "2035468030"
            }
        };
        
        var payUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/pay";
        var payJson = JsonSerializer.Serialize(payRequest);
        var payContent = new StringContent(payJson, Encoding.UTF8, "application/json");
        await _httpClient.PostAsync(payUrl, payContent);
        
        // Step 3: Query transaction status
        var queryUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/query/{transactionRef}";
        var queryResponse = await _httpClient.GetAsync(queryUrl);
        var queryResponseContent = await queryResponse.Content.ReadAsStringAsync();
        
        if (queryResponse.IsSuccessStatusCode)
        {
            var queryResult = JsonSerializer.Deserialize<RemitaTransactionQueryResponse>(queryResponseContent);
            return new RemitaPaymentResponse
            {
                Status = "SUCCESS",
                Rrr = queryResult?.Data?.Rrr ?? rrr,
                Amount = queryResult?.Data?.Amount ?? request.Amount,
                Paid = queryResult?.Data?.Paid ?? true,
                ReceiptUrl = queryResult?.Data?.Metadata?.ReceiptUrl ?? ""
            };
        }
        
        return new RemitaPaymentResponse
        {
            Status = "SUCCESS",
            Rrr = rrr,
            Amount = request.Amount,
            Paid = true,
            ReceiptUrl = ""
        };
    }



    public async Task<dynamic> ProcessTransactionWithAuthAsync(RemitaTransactionInitiateDto request)
    {
        try
        {
            // Extract userId from email or use customerId
            var userId = request.Email;
            
            // Validate PIN
            if (!string.IsNullOrEmpty(request.Pin))
            {
                var isPinValid = await _pinValidationService.ValidatePinAsync(userId, request.Pin);
                if (!isPinValid)
                {
                    return new { status = "03", message = "Invalid PIN", data = (object?)null };
                }
            }
            
            // Validate 2FA if enforced
            if (request.Enforce2FA)
            {
                if (string.IsNullOrEmpty(request.SecondFa) || string.IsNullOrEmpty(request.SecondFaType))
                {
                    return new { status = "04", message = "2FA required", data = (object?)null };
                }
                
                var is2FAValid = await _pinValidationService.Validate2FAAsync(userId, request.SecondFa, request.SecondFaType);
                if (!is2FAValid)
                {
                    return new { status = "05", message = "Invalid 2FA", data = (object?)null };
                }
            }
            
            // Generate RRR from timestamp
            var rrr = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            // Proceed with transaction processing
            return new { status = "00", message = "Transaction processed successfully", data = new { transactionRef = request.TransactionRef, rrr = rrr } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection error in ProcessTransactionWithAuthAsync");
            return new { status = "99", message = "Service temporarily unavailable. Please try again later.", data = (object?)null };
        }
    }

    public Task<dynamic> InitiatePaymentAsync(RemitaInitiatePaymentDto request)
    {
        var invoiceRequest = new RemitaInvoiceRequest
        {
            ServiceTypeId = _configuration["Remita:ServiceTypeId"] ?? string.Empty,
            Amount = request.Amount,
            OrderId = request.OrderId ?? Guid.NewGuid().ToString(),
            PayerName = request.PayerName,
            PayerEmail = request.PayerEmail,
            PayerPhone = request.PayerPhone,
            Description = request.Description
        };
        
        return Task.FromResult<dynamic>(new { Status = "SUCCESS", Data = invoiceRequest });
    }

    public async Task<dynamic> VerifyPaymentAsync(string rrr)
    {
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/verify/{rrr}";
            
            _logger.LogInformation("[OUTBOUND-{RequestId}] VerifyPaymentAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] VerifyPaymentAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                return new { Status = "ERROR", Message = "Payment verification failed", Data = responseContent };
            }
            
            try
            {
                var data = JsonSerializer.Deserialize<object>(responseContent);
                return new { Status = "SUCCESS", Data = data };
            }
            catch (JsonException)
            {
                return new { Status = "ERROR", Message = "Invalid response format", Data = responseContent };
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] VerifyPaymentAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            return new { Status = "ERROR", Message = ex.Message, Data = (object?)null };
        }
    }

    public async Task<dynamic> GetTransactionStatusAsync(string transactionId)
    {
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/status/{transactionId}";
        
        var response = await _httpClient.GetAsync(requestUrl);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        return new { Status = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR", Data = JsonSerializer.Deserialize<object>(responseContent) };
    }





    public async Task<dynamic> GetActiveBanksAsync()
    {
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/rpgsvc/v3/rpg/banks";
            
            _logger.LogInformation("[OUTBOUND-{RequestId}] GetActiveBanksAsync: POST {Url}", requestId, requestUrl);
            
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetActiveBanksAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            return new { Status = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR", Data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetActiveBanksAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<dynamic> ActivateMandateAsync(RemitaRrrPaymentRequest request)
    {
        await SetAuthHeaderAsync();
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/paymentnotification";
            
            var json = JsonSerializer.Serialize(request);
            _logger.LogInformation("[OUTBOUND-{RequestId}] ActivateMandateAsync: POST {Url} | Request: {Request}", requestId, requestUrl, json);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ActivateMandateAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            if (!response.IsSuccessStatusCode)
            {
                return new { status = "01", message = "Payment processing failed", data = responseContent };
            }
            
            try
            {
                return JsonSerializer.Deserialize<object>(responseContent);
            }
            catch (JsonException)
            {
                return new { status = "01", message = "Invalid response format", data = responseContent };
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ActivateMandateAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            return new { status = "99", message = "Service temporarily unavailable", data = ex.Message };
        }
    }

    public async Task<dynamic> GetRrrDetailsAsync(string rrr)
    {
        await SetAuthHeaderAsync();
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/lookup/{rrr}";
            
            _logger.LogInformation("[OUTBOUND-{RequestId}] GetRrrDetailsAsync: GET {Url}", requestId, requestUrl);
            
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] GetRrrDetailsAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            var result = JsonSerializer.Deserialize<dynamic>(responseContent);
            
            // Check for system malfunction and return user-friendly message
            if (result?.GetProperty("status").GetString() == "96")
            {
                return new { status = "01", message = "RRR not found or invalid. Please check the RRR number and try again.", data = (object?)null };
            }
            
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] GetRrrDetailsAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            return new { status = "99", message = "Unable to retrieve RRR details at this time. Please try again later.", data = (object?)null };
        }
    }

    public async Task<dynamic> ActivateRrrPaymentAsync(RemitaRrrPaymentRequest request)
    {
        await SetAuthHeaderAsync();
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/pay";
            
            var json = JsonSerializer.Serialize(request);
            _logger.LogInformation("[OUTBOUND-{RequestId}] ActivateRrrPaymentAsync: POST {Url} | Request: {Request}", requestId, requestUrl, json);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ActivateRrrPaymentAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            return JsonSerializer.Deserialize<object>(responseContent);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ActivateRrrPaymentAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public async Task<dynamic> ProcessRrrPaymentAsync(RemitaRrrPaymentRequest request)
    {
        await SetAuthHeaderAsync();
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var startTime = DateTime.UtcNow;
        
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/transaction/paymentnotification";
            
            var json = JsonSerializer.Serialize(request);
            _logger.LogInformation("[OUTBOUND-{RequestId}] ProcessRrrPaymentAsync: POST {Url} | Request: {Request}", requestId, requestUrl, json);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] ProcessRrrPaymentAsync: Status={StatusCode} | Duration={Duration}ms | Response: {Response}", 
                requestId, response.StatusCode, duration, responseContent);
            
            return JsonSerializer.Deserialize<object>(responseContent);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ProcessRrrPaymentAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }
}