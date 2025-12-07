using GovernmentCollections.Domain.DTOs.Remita;
using GovernmentCollections.Service.Services.Settlement;
using GovernmentCollections.Shared.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Remita.Transaction;

public class RemitaTransactionService : IRemitaTransactionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaTransactionService> _logger;
    private readonly IPinValidationService _pinValidationService;
    private readonly ISettlementService _settlementService;

    public RemitaTransactionService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaTransactionService> logger, 
        IPinValidationService pinValidationService, ISettlementService settlementService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _pinValidationService = pinValidationService;
        _settlementService = settlementService;
    }

    public async Task<dynamic> ProcessTransactionWithAuthAsync(RemitaTransactionInitiateDto request)
    {
        try
        {
            var userId = request.Email;
            
            if (!string.IsNullOrEmpty(request.Pin))
            {
                var isPinValid = await _pinValidationService.ValidatePinAsync(userId, request.Pin);
                if (!isPinValid)
                {
                    return new { status = "03", message = "Invalid PIN", data = (object?)null };
                }
            }
            
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
            
            var rrr = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            
            var settlementResult = await _settlementService.ProcessSettlementAsync(
                request.TransactionRef,
                request.AccountNumber,
                request.Amount,
                $"Remita Payment - {request.BillPaymentProductId}",
                "REMITA");
            
            if (!settlementResult.ResponseStatus)
            {
                _logger.LogWarning("Settlement failed for transaction {TransactionRef}: {Message}", request.TransactionRef, settlementResult.ResponseMessage);
                return new { status = "06", message = "Payment processed but settlement failed", data = new { transactionRef = request.TransactionRef, rrr = rrr, settlementStatus = settlementResult.ResponseCode } };
            }
            
            return new { status = "00", message = "Transaction processed successfully", data = new { transactionRef = request.TransactionRef, rrr = rrr, settlementRef = settlementResult.ResponseData } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection error in ProcessTransactionWithAuthAsync");
            return new { status = "99", message = "Service temporarily unavailable. Please try again later.", data = (object?)null };
        }
    }

    public async Task<dynamic> GetTransactionStatusAsync(string transactionId)
    {
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/status/{transactionId}";
        
        var response = await _httpClient.GetAsync(requestUrl);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        return new { Status = response.IsSuccessStatusCode ? "SUCCESS" : "ERROR", Data = JsonSerializer.Deserialize<object>(responseContent) ?? new object() };
    }
}