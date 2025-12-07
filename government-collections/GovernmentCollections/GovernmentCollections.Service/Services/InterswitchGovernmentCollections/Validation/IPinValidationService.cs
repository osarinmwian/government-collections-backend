namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Validation;

public interface IPinValidationService
{
    Task<bool> ValidatePinAsync(string userId, string pin);
    Task<bool> Validate2FAAsync(string userId, string secondFa, string secondFaType);
}