namespace GovernmentCollections.Service.Services.Remita.Authentication;

public interface IRemitaAuthenticationService
{
    Task<string> GetAccessTokenAsync();
    Task SetAuthHeaderAsync(HttpClient httpClient);
}