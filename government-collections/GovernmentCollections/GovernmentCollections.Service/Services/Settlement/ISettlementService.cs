using GovernmentCollections.Domain.DTOs.Settlement;

namespace GovernmentCollections.Service.Services.Settlement;

public interface ISettlementService
{
    Task<DebitResponse> ProcessSettlementAsync(string transactionRef, string accountNumber, decimal amount, string narration, string paymentGateway, CancellationToken cancellationToken = default);
    Task<DebitResponse> ProcessSettlementAsync(SettlementRequest request, CancellationToken cancellationToken = default);
}