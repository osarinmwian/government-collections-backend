using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Domain.Settings;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections;

public class InterswitchAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InterswitchAuthService> _logger;
    private readonly IMemoryCache _cache;
    private readonly InterswitchSettings _settings;
    private const string TOKEN_CACHE_KEY = "interswitch_token";

    public InterswitchAuthService(
        HttpClient httpClient,
        ILogger<InterswitchAuthService> logger,
        IMemoryCache cache,
        IOptions<InterswitchSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _settings = settings.Value;
    }

    public async Task<InterswitchAuthResponse> AuthenticateAsync()
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var startTime = DateTime.UtcNow;
        
        try
        {
            var requestUrl = $"{_settings.BaseUrl}/passport/oauth/token";
            
            _logger.LogInformation("[OUTBOUND-{RequestId}] AuthenticateAsync: POST {Url}", requestId, requestUrl);
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.UserName}:{_settings.Password}"));
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials")
            };
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            
            _logger.LogInformation("[INBOUND-{RequestId}] AuthenticateAsync: Status={StatusCode} | Duration={Duration}ms", 
                requestId, response.StatusCode, duration);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[ERROR-{RequestId}] Authentication failed. Status: {StatusCode}", requestId, response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var authResponse = JsonSerializer.Deserialize<InterswitchAuthResponse>(responseContent);

            if (authResponse != null && !string.IsNullOrEmpty(authResponse.AccessToken))
            {
                _logger.LogInformation("[SUCCESS-{RequestId}] Token received, expires in: {ExpiresIn} seconds", requestId, authResponse.ExpiresIn);
                
                var cacheExpiry = TimeSpan.FromSeconds(authResponse.ExpiresIn - _settings.TokenExpiryBuffer);
                _cache.Set(TOKEN_CACHE_KEY, authResponse, cacheExpiry);
                
                return authResponse;
            }

            throw new InvalidOperationException("Authentication failed - no access token received");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            _logger.LogError(ex, "[ERROR-{RequestId}] AuthenticateAsync: Duration={Duration}ms | Exception: {Message}", requestId, duration, ex.Message);
            throw;
        }
    }

    public Task<bool> IsTokenValidAsync()
    {
        return Task.FromResult(_cache.TryGetValue(TOKEN_CACHE_KEY, out _));
    }

    public async Task<string> GetValidTokenAsync()
    {
        _logger.LogInformation("[TOKEN] Checking for cached token...");
        
        if (_cache.TryGetValue(TOKEN_CACHE_KEY, out InterswitchAuthResponse? cachedAuth) && cachedAuth != null)
        {
            _logger.LogInformation("[TOKEN] Using cached token");
            return cachedAuth.AccessToken;
        }

        _logger.LogInformation("[TOKEN] No cached token found, authenticating...");
        var authResponse = await AuthenticateAsync();
        return authResponse.AccessToken;
    }

    public async Task<string> GetTerminalIdAsync()
    {
        _logger.LogInformation("[TERMINAL] Getting terminal ID...");
        
        if (_cache.TryGetValue(TOKEN_CACHE_KEY, out InterswitchAuthResponse? cachedAuth) && cachedAuth != null)
        {
            _logger.LogInformation("[TERMINAL] Using cached terminal ID: {TerminalId}", cachedAuth.TerminalId);
            return cachedAuth.TerminalId;
        }

        _logger.LogInformation("[TERMINAL] No cached auth found, authenticating...");
        var authResponse = await AuthenticateAsync();
        return authResponse.TerminalId;
    }
}