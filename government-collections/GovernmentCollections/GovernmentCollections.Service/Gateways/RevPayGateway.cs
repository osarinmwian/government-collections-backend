using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.Settings;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Gateways;

public class RevPayGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly RevPaySettings _settings;
    private readonly ILogger<RevPayGateway> _logger;

    public RevPayGateway(HttpClient httpClient, RevPaySettings settings, ILogger<RevPayGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("ApiKey", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("ClientId", _settings.ClientId);
    }

    public async Task<BillInquiryResponseDto> InquireBillAsync(BillInquiryDto request)
    {
        try
        {
            var payload = new
            {
                customerReference = request.CustomerReference,
                paymentType = request.PaymentType.ToString(),
                state = _settings.State
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("bill-inquiry", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<BillInquiryResponseDto>(responseContent);
                return result ?? new BillInquiryResponseDto { IsValid = false, Message = "Invalid response" };
            }

            return new BillInquiryResponseDto { IsValid = false, Message = "Bill inquiry failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RevPay bill inquiry");
            return new BillInquiryResponseDto { IsValid = false, Message = "Service unavailable" };
        }
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request)
    {
        try
        {
            var payload = new
            {
                transactionReference = Guid.NewGuid().ToString(),
                customerReference = request.CustomerReference,
                payerName = request.PayerName,
                payerEmail = request.PayerEmail,
                payerPhone = request.PayerPhone,
                amount = request.Amount,
                description = request.Description,
                paymentType = request.PaymentType.ToString(),
                state = _settings.State
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("process-payment", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaymentResponseDto>(responseContent);
                return result ?? new PaymentResponseDto { Status = "Failed", Message = "Invalid response" };
            }

            return new PaymentResponseDto { Status = "Failed", Message = "Payment processing failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RevPay payment processing");
            return new PaymentResponseDto { Status = "Failed", Message = "Service unavailable" };
        }
    }

    public async Task<PaymentResponseDto> VerifyPaymentAsync(string transactionReference)
    {
        try
        {
            var response = await _httpClient.GetAsync($"verify-payment/{transactionReference}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<PaymentResponseDto>(responseContent);
                return result ?? new PaymentResponseDto { Status = "Failed", Message = "Invalid response" };
            }

            return new PaymentResponseDto { Status = "Failed", Message = "Payment verification failed" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RevPay payment verification");
            return new PaymentResponseDto { Status = "Failed", Message = "Service unavailable" };
        }
    }
}