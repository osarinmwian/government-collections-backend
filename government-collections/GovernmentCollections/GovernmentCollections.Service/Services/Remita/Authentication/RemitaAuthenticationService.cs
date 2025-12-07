using GovernmentCollections.Domain.DTOs.Remita;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace GovernmentCollections.Service.Services.Remita.Authentication;

public class RemitaAuthenticationService : IRemitaAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public RemitaAuthenticationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        var username = _configuration["Remita:Username"];
        var password = _configuration["Remita:Password"];
        var baseUrl = _configuration["Remita:BaseUrl"];
        var tokenUrl = $"{baseUrl}/remita/exapp/api/v1/send/api/uaasvc/uaa/token";

        var authRequest = new { username, password };
        var json = JsonSerializer.Serialize(authRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(tokenUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var authResponse = JsonSerializer.Deserialize<RemitaAuthResponse>(responseContent);
            if (authResponse?.Data?.Count > 0)
            {
                _accessToken = authResponse.Data[0].AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse.Data[0].ExpiresIn - 60);
                return _accessToken;
            }
        }
        throw new Exception("Failed to authenticate with Remita");
    }

    public async Task SetAuthHeaderAsync(HttpClient httpClient)
    {
        var token = await GetAccessTokenAsync();
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}