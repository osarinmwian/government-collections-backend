using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GovernmentCollections.Service.Services.Remita.Authentication;
using Microsoft.Extensions.Configuration;

namespace GovernmentCollections.Service.Services.Remita.Invoice;

public class RemitaInvoiceService : IRemitaInvoiceService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IRemitaAuthenticationService _authService;

    public RemitaInvoiceService(HttpClient httpClient, IConfiguration configuration, IRemitaAuthenticationService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authService = authService;
    }

    public async Task<RemitaInvoiceResponse> GenerateInvoiceAsync(RemitaInvoiceRequest request)
    {
        var token = await _authService.GetAccessTokenAsync();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _httpClient.DefaultRequestHeaders.Add("publicKey", _configuration["Remita:PublicKey"]);
        _httpClient.DefaultRequestHeaders.Add("TIMESTAMP", timestamp);

        var hash = GenerateHash(request, _configuration["Remita:SecretKey"]);
        _httpClient.DefaultRequestHeaders.Add("TXN_HASH", hash);

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/remita/exapp/api/v1/send/api/echannelsvc/merchant/api/paymentinit", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RemitaInvoiceResponse>(responseContent) ?? new RemitaInvoiceResponse();
    }

    public async Task<RemitaPaymentStatusResponse> VerifyPaymentAsync(string rrr)
    {
        var publicKey = _configuration["Remita:PublicKey"];
        var secretKey = _configuration["Remita:SecretKey"];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var hash = GenerateVerificationHash(rrr, secretKey ?? string.Empty);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("publicKey", publicKey);
        _httpClient.DefaultRequestHeaders.Add("TIMESTAMP", timestamp);
        _httpClient.DefaultRequestHeaders.Add("TXN_HASH", hash);

        var response = await _httpClient.GetAsync($"/remita/exapp/api/v1/send/api/echannelsvc/merchant/api/paymentstatus/{rrr}");
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RemitaPaymentStatusResponse>(responseContent) ?? new RemitaPaymentStatusResponse();
    }

    private string GenerateHash(RemitaInvoiceRequest request, string? secretKey)
    {
        var hashString = $"{request.ServiceTypeId}{request.Amount}{request.OrderId}{secretKey ?? string.Empty}";
        return ComputeSha512Hash(hashString);
    }

    private string GenerateVerificationHash(string rrr, string secretKey)
    {
        var hashString = $"{rrr}{secretKey}";
        return ComputeSha512Hash(hashString);
    }

    private string ComputeSha512Hash(string input)
    {
        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha512.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}

public class RemitaInvoiceRequest
{
    public string ServiceTypeId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RemitaInvoiceResponse
{
    public string StatusCode { get; set; } = string.Empty;
    public string RRR { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
}

public class RemitaPaymentStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public string RRR { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Message { get; set; } = string.Empty;
}