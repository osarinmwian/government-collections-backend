using GovernmentCollections.Domain.DTOs.RevPay;
using GovernmentCollections.Domain.Settings;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.RevPay.Payment;

public class RevPayPaymentService : IRevPayPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly RevPaySettings _settings;
    private readonly ILogger<RevPayPaymentService> _logger;

    public RevPayPaymentService(HttpClient httpClient, RevPaySettings settings, ILogger<RevPayPaymentService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<dynamic> ProcessPaymentAsync(RevPayPaymentRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/Payment", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "Payment processed successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment");
            return new { status = "01", message = "Failed to process payment", data = (object?)null };
        }
    }

    public async Task<dynamic> GenerateWebGuidAsync(RevPayWebGuidRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/WebGuid", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "WebGuid generated successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating WebGuid");
            return new { status = "01", message = "Failed to generate WebGuid", data = (object?)null };
        }
    }

    public async Task<dynamic> GetReceiptAsync(RevPayReceiptRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/ReceiptByPaymentRef", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "Receipt retrieved successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting receipt");
            return new { status = "01", message = "Failed to get receipt", data = (object?)null };
        }
    }
}