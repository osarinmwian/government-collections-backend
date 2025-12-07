namespace GovernmentCollections.Shared.Validation;

public interface IPinValidationService
{
    Task<bool> ValidatePinAsync(string customerId, string pin);
    Task<bool> Validate2FAAsync(string customerId, string secondFa, string secondFaType);
}