using GovernmentCollections.Domain.DTOs;

namespace GovernmentCollections.Service.Gateways;

public interface IPaymentGateway
{
    Task<BillInquiryResponseDto> InquireBillAsync(BillInquiryDto request);
    Task<PaymentResponseDto> ProcessPaymentAsync(PaymentRequestDto request);
    Task<PaymentResponseDto> VerifyPaymentAsync(string transactionReference);
}