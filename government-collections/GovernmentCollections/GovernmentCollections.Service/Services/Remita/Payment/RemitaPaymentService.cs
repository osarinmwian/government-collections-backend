using GovernmentCollections.Domain.DTOs.Remita;
using GovernmentCollections.Service.Services.Remita.Authentication;
using GovernmentCollections.Service.Services.Settlement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Remita.Payment;

public class RemitaPaymentService : IRemitaPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaPaymentService> _logger;
    private readonly IRemitaAuthenticationService _authService;
    private readonly ISettlementService _settlementService;

    public RemitaPaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaPaymentService> logger, 
        IRemitaAuthenticationService authService, ISettlementService settlementService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authService = authService;
        _settlementService = settlementService;
    }

    public async Task<RemitaPaymentResponse> ProcessPaymentAsync(RemitaPaymentRequest request)
    {
        await _authService.SetAuthHeaderAsync(_httpClient);
        var transactionRef = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        
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
        
        if (!initResponse.IsSuccessStatusCode)
        {
            return new RemitaPaymentResponse { Status = "ERROR", Rrr = "", Amount = 0, Paid = false, ReceiptUrl = "" };
        }
        
        var rrr = request.TransactionRef;
        
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
            
            return new { Status = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR", Data = JsonSerializer.Deserialize<object>(responseContent) ?? new object() };
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
        await _authService.SetAuthHeaderAsync(_httpClient);
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
                return JsonSerializer.Deserialize<object>(responseContent) ?? new { status = "01", message = "Invalid response format", data = responseContent };
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
        await _authService.SetAuthHeaderAsync(_httpClient);
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
            
            if (result?.GetProperty("status").GetString() == "96")
            {
                return new { status = "01", message = "RRR not found or invalid. Please check the RRR number and try again.", data = (object?)null };
            }
            
            return result ?? new object();
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
        await _authService.SetAuthHeaderAsync(_httpClient);
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
            
            return JsonSerializer.Deserialize<object>(responseContent) ?? new object();
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
        await _authService.SetAuthHeaderAsync(_httpClient);
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
            
            var result = JsonSerializer.Deserialize<dynamic>(responseContent);
            
            if (response.IsSuccessStatusCode && result?.GetProperty("status").GetString() == "00")
            {
                var settlementResult = await _settlementService.ProcessSettlementAsync(
                    request.Rrr,
                    request.AccountNumber,
                    request.Amount,
                    $"Remita RRR Payment - {request.Rrr}",
                    "REMITA");
                _logger.LogInformation("[SETTLEMENT-{RequestId}] Settlement result: {Status} - {Message}", requestId, settlementResult.ResponseStatus, settlementResult.ResponseMessage);
            }
            
            return result ?? new object();
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] ProcessRrrPaymentAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }
}