namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Authentication;

public interface IInterswitchAuthenticationService
{
    Task<string> GetAccessTokenAsync();
    Task SetAuthHeaderAsync(HttpClient httpClient);
}