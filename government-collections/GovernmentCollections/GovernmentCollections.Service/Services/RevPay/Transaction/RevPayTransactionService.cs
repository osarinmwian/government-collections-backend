using GovernmentCollections.Domain.DTOs.RevPay;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Services.RevPay.Validation;
using GovernmentCollections.Service.Services.Settlement;
using GovernmentCollections.Service.Services.RevPay.Payment;
using Microsoft.Extensions.Logging;

namespace GovernmentCollections.Service.Services.RevPay.Transaction;

public class RevPayTransactionService : IRevPayTransactionService
{
    private readonly IRevPayPaymentService _paymentService;
    private readonly IPinValidationService _pinValidationService;
    private readonly ISettlementService _settlementService;
    private readonly RevPaySettings _settings;
    private readonly ILogger<RevPayTransactionService> _logger;

    public RevPayTransactionService(
        IRevPayPaymentService paymentService,
        IPinValidationService pinValidationService,
        ISettlementService settlementService,
        RevPaySettings settings,
        ILogger<RevPayTransactionService> logger)
    {
        _paymentService = paymentService;
        _pinValidationService = pinValidationService;
        _settlementService = settlementService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<dynamic> ProcessTransactionWithAuthAsync(RevPayTransactionRequest request)
    {
        try
        {
            // Validate PIN and 2FA
            var pinValid = await _pinValidationService.ValidatePinAsync(request.AccountNumber, request.Pin);
            if (!pinValid)
            {
                return new { status = "01", message = "Invalid PIN", data = (object?)null };
            }

            if (request.Enforce2FA)
            {
                var twoFaValid = await _pinValidationService.Validate2FAAsync(
                    request.AccountNumber, request.SecondFa, request.SecondFaType);
                if (!twoFaValid)
                {
                    return new { status = "01", message = "Invalid 2FA", data = (object?)null };
                }
            }

            // Generate WebGuid first
            var webGuidRequest = new RevPayWebGuidRequest
            {
                Pid = request.Pid,
                Amount = request.Amount.ToString(),
                State = _settings.State,
                ClientId = _settings.ClientId
            };

            var webGuidResponse = await _paymentService.GenerateWebGuidAsync(webGuidRequest);
            if (webGuidResponse.status != "00")
            {
                return webGuidResponse;
            }

            // Process payment
            var paymentRequest = new RevPayPaymentRequest
            {
                WebGuid = "", // Extract from webGuidResponse
                AmountPaid = request.Amount.ToString(),
                PaymentRef = request.TransactionRef,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var paymentResponse = await _paymentService.ProcessPaymentAsync(paymentRequest);
            
            // Process settlement if payment successful
            if (paymentResponse.status == "00")
            {
                await _settlementService.ProcessSettlementAsync(new Domain.DTOs.Settlement.SettlementRequest
                {
                    TransactionReference = request.TransactionRef,
                    Amount = request.Amount,
                    AccountNumber = request.AccountNumber,
                    Channel = request.Channel,
                    PaymentGateway = "RevPay"
                });
            }

            return paymentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction with auth");
            return new { status = "01", message = "Transaction processing failed", data = (object?)null };
        }
    }
}