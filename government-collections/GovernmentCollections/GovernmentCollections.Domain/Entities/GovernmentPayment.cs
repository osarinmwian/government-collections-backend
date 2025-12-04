using GovernmentCollections.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace GovernmentCollections.Domain.Entities;

public class GovernmentPayment
{
    [Key]
    public int Id { get; set; }

    public string TransactionReference { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string PayerPhone { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public PaymentGateway Gateway { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string GatewayReference { get; set; } = string.Empty;
    public string GatewayResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}