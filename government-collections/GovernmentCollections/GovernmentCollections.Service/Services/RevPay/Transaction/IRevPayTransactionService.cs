using GovernmentCollections.Domain.DTOs.RevPay;

namespace GovernmentCollections.Service.Services.RevPay.Transaction;

public interface IRevPayTransactionService
{
    Task<dynamic> ProcessTransactionWithAuthAsync(RevPayTransactionRequest request);
}