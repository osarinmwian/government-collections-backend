using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GovernmentCollections.Service.Services.Remita;

public class RemitaAuthService : IRemitaAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemitaAuthService> _logger;
    private string? _cachedToken;
    private DateTime _tokenExpiry;

    public RemitaAuthService(HttpClient httpClient, IConfiguration configuration, ILogger<RemitaAuthService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        var tokenRequest = new
        {
            username = _configuration["Remita:Username"],
            password = _configuration["Remita:Password"]
        };

        var json = JsonSerializer.Serialize(tokenRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var baseUrl = _configuration["Remita:BaseUrl"];
        var requestUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/uaasvc/uaa/token";
        
        var response = await _httpClient.PostAsync(requestUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        var tokenResponse = JsonSerializer.Deserialize<RemitaTokenResponse>(responseContent);

        if (tokenResponse?.Data?.Any() == true)
        {
            _cachedToken = tokenResponse.Data[0].AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.Data[0].ExpiresIn - 300);
            return _cachedToken;
        }

        throw new Exception("Failed to obtain access token from Remita");
    }

    private class RemitaTokenResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public List<TokenData> Data { get; set; } = new();
    }

    private class TokenData
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}