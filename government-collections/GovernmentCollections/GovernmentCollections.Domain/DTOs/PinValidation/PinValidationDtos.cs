using GovernmentCollections.Domain.DTOs;

namespace GovernmentCollections.Domain.DTOs.PinValidation;

public class PinValidationDto
{
    public string UserId { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}

public class PaymentWithPinDto : PaymentRequestDto
{
    public string Pin { get; set; } = string.Empty;
}