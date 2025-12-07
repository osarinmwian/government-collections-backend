using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GovernmentCollections.Service.Services.Remita.Gateway;

public class RemitaPaymentGatewayService : IRemitaPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaPaymentGatewayService> _logger;

    public RemitaPaymentGatewayService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaPaymentGatewayService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RemitaTransactionStatusResponse> VerifyTransactionAsync(string transactionId)
    {
        var secretKey = _configuration["Remita:SecretKey"];
        var publicKey = _configuration["Remita:PublicKey"];
        var hash = GenerateTransactionHash(transactionId, secretKey);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("publicKey", publicKey);
        _httpClient.DefaultRequestHeaders.Add("TXN_HASH", hash);
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

        var response = await _httpClient.GetAsync($"/payment/v1/payment/query/{transactionId}");
        var responseContent = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<RemitaTransactionStatusResponse>(responseContent) ?? new RemitaTransactionStatusResponse();
    }

    public string GenerateCheckoutHash(RemitaCheckoutRequest request)
    {
        var secretKey = _configuration["Remita:SecretKey"];
        var hashString = $"{request.PublicKey}{request.TransactionId}{request.Amount}{secretKey}";
        return ComputeSha512Hash(hashString);
    }

    private string GenerateTransactionHash(string transactionId, string? secretKey)
    {
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("Remita SecretKey is not configured");
            
        var hashString = $"{transactionId}{secretKey}";
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

public class RemitaCheckoutRequest
{
    public string PublicKey { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Channels { get; set; } = Array.Empty<string>();
}

public class RemitaTransactionStatusResponse
{
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMsg { get; set; } = string.Empty;
    public List<TransactionData> ResponseData { get; set; } = new();
}

public class TransactionData
{
    public string PaymentReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentState { get; set; } = string.Empty;
    public string PaymentDate { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}