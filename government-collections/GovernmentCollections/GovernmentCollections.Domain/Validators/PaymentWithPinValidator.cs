using FluentValidation;
using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.DTOs.PinValidation;

namespace GovernmentCollections.Domain.Validators;

public class PaymentWithPinValidator : AbstractValidator<PaymentWithPinDto>
{
    public PaymentWithPinValidator()
    {
        Include(new PaymentRequestValidator());
        
        RuleFor(x => x.Pin)
            .NotEmpty()
            .WithMessage("PIN is required")
            .Length(4, 6)
            .WithMessage("PIN must be 4-6 digits");
    }
}