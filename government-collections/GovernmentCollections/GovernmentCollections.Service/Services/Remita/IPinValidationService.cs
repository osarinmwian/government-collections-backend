namespace GovernmentCollections.Service.Services.Remita;

public interface IPinValidationService
{
    Task<bool> ValidatePinAsync(string userId, string pin);
    Task<bool> Validate2FAAsync(string userId, string secondFa, string secondFaType);
}