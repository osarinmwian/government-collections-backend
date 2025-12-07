using GovernmentCollections.Domain.DTOs.RevPay;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Utilities;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.RevPay.BillType;

public class RevPayBillTypeService : IRevPayBillTypeService
{
    private readonly HttpClient _httpClient;
    private readonly RevPaySettings _settings;
    private readonly ILogger<RevPayBillTypeService> _logger;

    public RevPayBillTypeService(HttpClient httpClient, RevPaySettings settings, ILogger<RevPayBillTypeService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task<dynamic> GetBillTypesAsync()
    {
        try
        {
            var request = new RevPayBillTypeRequest
            {
                State = _settings.State,
                ClientId = _settings.ClientId,
                Hash = HashUtility.ComputeSHA512Hash(_settings.ApiKey + _settings.State)
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/Billtype", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "Bill types retrieved successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bill types");
            return new { status = "01", message = "Failed to get bill types", data = (object?)null };
        }
    }

    public async Task<dynamic> ValidateReferenceAsync(RevPayValidateRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/Validate", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "Reference validated successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reference");
            return new { status = "01", message = "Failed to validate reference", data = (object?)null };
        }
    }

    public async Task<dynamic> VerifyPidAsync(RevPayPidVerificationRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/interface/VerifyPid", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return new { status = "00", message = "PID verified successfully", data = JsonSerializer.Deserialize<object>(responseContent) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PID");
            return new { status = "01", message = "Failed to verify PID", data = (object?)null };
        }
    }
}