using GovernmentCollections.Domain.DTOs.Settlement;
using GovernmentCollections.Domain.Settings;
using GovernmentCollections.Service.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Settlement;

public class SettlementService : ISettlementService
{
    private readonly HttpClient _httpClient;
    private readonly EncryptionSettings _encryptionSettings;
    private readonly FundTransferApiUrl _fundTransferApiUrl;
    private readonly SettlementAccountSettings _settlementSettings;
    private readonly ILogger<SettlementService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SettlementService(
        HttpClient httpClient,
        IOptions<EncryptionSettings> encryptionOptions,
        IOptions<FundTransferApiUrl> fundTransferApiUrl,
        IOptions<SettlementAccountSettings> settlementSettings,
        ILogger<SettlementService> logger)
    {
        _httpClient = httpClient;
        _encryptionSettings = encryptionOptions.Value;
        _fundTransferApiUrl = fundTransferApiUrl.Value;
        _settlementSettings = settlementSettings.Value;
        _logger = logger;

        if (_httpClient.Timeout == default)
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<DebitResponse> ProcessSettlementAsync(string transactionRef, string accountNumber, decimal amount, string narration, string paymentGateway, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_fundTransferApiUrl?.ApiUrl))
                throw new InvalidOperationException("Fund transfer API URL is not configured");

            var debitRequest = new DebitRequest
            {
                TransactionRef = $"STL{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Amount = amount,
                DebitAccount = accountNumber,
                CreditAccount = _settlementSettings.SettlementCreditAccount,
                Narration = $"{paymentGateway} Settlement - {narration}",
                Commissions = _settlementSettings.Commissions,
                CommissionCode = _settlementSettings.CommissionCode,
                SessionId = transactionRef,
                T24TransactionType = _settlementSettings.T24TransactionType,
                T24DistributionName = _settlementSettings.T24DistributionName
            };

            _logger.LogInformation(
                "Processing settlement - TransactionRef: {TransactionRef}, DebitAccount: {DebitAccount}, Amount: {Amount}",
                transactionRef, MaskAccount(accountNumber), amount);

            var cipher = CryptoHelper.EncryptJson(JsonSerializer.SerializeToElement(debitRequest, JsonOptions), _encryptionSettings.SecretKey);

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, _fundTransferApiUrl.ApiUrl)
            {
                Content = new StringContent(cipher, Encoding.UTF8, "text/plain")
            };
            httpReq.Headers.Accept.Clear();
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpReq.Headers.Add("X-USERNAME", _fundTransferApiUrl.Username);

            using var httpResp = await _httpClient.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var body = await httpResp.Content.ReadAsStringAsync(cancellationToken);

            if (!httpResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Settlement API returned non-success status: {StatusCode}. Body: {Body}", (int)httpResp.StatusCode, body);
                return new DebitResponse
                {
                    ResponseStatus = false,
                    ResponseCode = ((int)httpResp.StatusCode).ToString(),
                    ResponseMessage = string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)httpResp.StatusCode}" : body,
                    ResponseData = string.Empty
                };
            }

            var responseCipher = ExtractCipher(body);
            if (string.IsNullOrWhiteSpace(responseCipher))
            {
                return new DebitResponse
                {
                    ResponseStatus = false,
                    ResponseCode = "99",
                    ResponseMessage = "Empty encrypted response from settlement API",
                    ResponseData = string.Empty
                };
            }

            try
            {
                var decryptedJson = CryptoHelper.DecryptJson(responseCipher, _encryptionSettings.SecretKey);
                var response = JsonSerializer.Deserialize<DebitResponse>(decryptedJson.GetRawText(), JsonOptions);
                
                if (response != null)
                {
                    _logger.LogInformation("Settlement processed - Status: {Status}, Message: {Message}", response.ResponseStatus, response.ResponseMessage);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt/deserialize settlement response");
            }

            return new DebitResponse
            {
                ResponseStatus = false,
                ResponseCode = "99",
                ResponseMessage = "Unable to process settlement response",
                ResponseData = string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Settlement processing failed for transaction {TransactionRef}", transactionRef);
            return new DebitResponse
            {
                ResponseStatus = false,
                ResponseCode = "99",
                ResponseMessage = "Settlement processing failed due to system error",
                ResponseData = string.Empty
            };
        }
    }

    private static string MaskAccount(string? account)
    {
        if (string.IsNullOrWhiteSpace(account)) return string.Empty;
        var trimmed = account.Trim();
        if (trimmed.Length <= 4) return new string('*', trimmed.Length);
        var last4 = trimmed[^4..];
        return new string('*', trimmed.Length - 4) + last4;
    }

    private string ExtractCipher(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.String)
                    return d.GetString() ?? string.Empty;
                if (root.TryGetProperty("Data", out var D) && D.ValueKind == JsonValueKind.String)
                    return D.GetString() ?? string.Empty;
            }
            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? string.Empty;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse response body as JSON");
        }
        return body?.Trim() ?? string.Empty;
    }

    public async Task<DebitResponse> ProcessSettlementAsync(SettlementRequest request, CancellationToken cancellationToken = default)
    {
        return await ProcessSettlementAsync(
            request.TransactionReference,
            request.AccountNumber,
            request.Amount,
            $"{request.PaymentGateway} payment",
            request.PaymentGateway,
            cancellationToken);
    }
}