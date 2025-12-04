namespace GovernmentCollections.Service.Services.PinValidation;

public interface IPinValidationService
{
    Task<bool> ValidatePinAsync(string userId, string pin);
}