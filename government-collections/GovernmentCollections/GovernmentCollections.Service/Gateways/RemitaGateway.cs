using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.Settings;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Gateways;

public class RemitaGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly RemitaSettings _settings;
    private readonly ILogger<RemitaGateway> _logger;

    public RemitaGateway(HttpClient httpClient, RemitaSettings settings, ILogger<RemitaGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Token}");
    }

    public async Task<BillInquiryResponseDto> InquireBillAsync(BillInquiryDto request)
    {
        try
        {
            var payload = new
            {
                customerReference = request.CustomerReference,
                paymentType = request.PaymentType.ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("remita/bill-inquiry", content);
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
            _logger.LogError(ex, "Error during Remita bill inquiry");
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
                paymentType = request.PaymentType.ToString()
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("remita/process-payment", content);
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
            _logger.LogError(ex, "Error during Remita payment processing");
            return new PaymentResponseDto { Status = "Failed", Message = "Service unavailable" };
        }
    }

    public async Task<PaymentResponseDto> VerifyPaymentAsync(string transactionReference)
    {
        try
        {
            var response = await _httpClient.GetAsync($"remita/verify-payment/{transactionReference}");
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
            _logger.LogError(ex, "Error during Remita payment verification");
            return new PaymentResponseDto { Status = "Failed", Message = "Service unavailable" };
        }
    }
}