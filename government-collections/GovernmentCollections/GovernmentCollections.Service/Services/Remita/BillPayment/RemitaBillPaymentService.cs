using GovernmentCollections.Domain.DTOs.Remita;
using GovernmentCollections.Service.Services.Remita.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Remita.BillPayment;

public class RemitaBillPaymentService : IRemitaBillPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaBillPaymentService> _logger;
    private readonly IRemitaAuthenticationService _authService;

    public RemitaBillPaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaBillPaymentService> logger, IRemitaAuthenticationService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _authService = authService;
    }

    public async Task<List<RemitaBillerDto>> GetBillersAsync()
    {
        try
        {
            var baseUrl = _configuration["Remita:BaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("Remita:BaseUrl not configured, returning empty list");
                return new List<RemitaBillerDto>();
            }
            
            var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/billers";
            _logger.LogInformation("Fetching billers from: {RequestUrl}", requestUrl);
            
            await _authService.SetAuthHeaderAsync(_httpClient);
            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Billers API response: Status={StatusCode}, Content={Content}", response.StatusCode, responseContent);
            
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
            
            _logger.LogWarning("Billers API failed with status {StatusCode}, returning empty list", response.StatusCode);
            return new List<RemitaBillerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching billers, returning empty list");
            return new List<RemitaBillerDto>();
            //   return GetMockBillers();
        }
    }
    
    // private List<RemitaBillerDto> GetMockBillers()
    // {
    //     return new List<RemitaBillerDto>
    //     {
    //         new RemitaBillerDto { BillerId = "FIRS001", Name = "Federal Inland Revenue Service", Logo = "", Category = "Tax" },
    //         new RemitaBillerDto { BillerId = "LASIRS001", Name = "Lagos State Internal Revenue Service", Logo = "", Category = "Tax" },
    //         new RemitaBillerDto { BillerId = "CUSTOMS001", Name = "Nigeria Customs Service", Logo = "", Category = "Customs" },
    //         new RemitaBillerDto { BillerId = "FRSC001", Name = "Federal Road Safety Corps", Logo = "", Category = "License" }
    //     };
    // }

    public async Task<RemitaBillerDetailsDto> GetBillerByIdAsync(string billerId)
    {
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/bgatesvc/v3/billpayment/biller/{billerId}/products";
        
        await _authService.SetAuthHeaderAsync(_httpClient);
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
}