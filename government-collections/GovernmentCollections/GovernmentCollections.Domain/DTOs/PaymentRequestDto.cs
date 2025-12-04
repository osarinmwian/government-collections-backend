using GovernmentCollections.Domain.Enums;

namespace GovernmentCollections.Domain.DTOs;

public class PaymentRequestDto
{
    public string CustomerReference { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class PaymentResponseDto
{
    public string TransactionReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string GatewayReference { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
}

public class BillInquiryDto
{
    public string CustomerReference { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentGateway Gateway { get; set; }
}

public class BillInquiryResponseDto
{
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}