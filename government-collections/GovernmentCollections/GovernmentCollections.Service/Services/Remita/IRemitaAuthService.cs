namespace GovernmentCollections.Service.Services.Remita;

public interface IRemitaAuthService
{
    Task<string> GetAccessTokenAsync();
}