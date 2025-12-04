using FluentValidation;
using GovernmentCollections.Domain.DTOs;

namespace GovernmentCollections.Domain.Validators;

public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.CustomerReference)
            .NotEmpty()
            .WithMessage("Customer reference is required");

        RuleFor(x => x.PayerName)
            .NotEmpty()
            .WithMessage("Payer name is required");

        RuleFor(x => x.PayerEmail)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email is required");

        RuleFor(x => x.PayerPhone)
            .NotEmpty()
            .WithMessage("Phone number is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}