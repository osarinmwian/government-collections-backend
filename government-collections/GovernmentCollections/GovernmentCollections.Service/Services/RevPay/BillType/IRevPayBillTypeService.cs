using GovernmentCollections.Domain.DTOs.RevPay;

namespace GovernmentCollections.Service.Services.RevPay.BillType;

public interface IRevPayBillTypeService
{
    Task<dynamic> GetBillTypesAsync();
    Task<dynamic> ValidateReferenceAsync(RevPayValidateRequest request);
    Task<dynamic> VerifyPidAsync(RevPayPidVerificationRequest request);
}