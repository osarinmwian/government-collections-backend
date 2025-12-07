using GovernmentCollections.Domain.DTOs.Interswitch;

namespace GovernmentCollections.Service.Services.InterswitchGovernmentCollections.Validation;

public interface IInterswitchValidationService
{
    Task<InterswitchCustomerValidationResponse> ValidateCustomersAsync(InterswitchCustomerValidationBatchRequest request);
}