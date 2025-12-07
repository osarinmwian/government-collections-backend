using GovernmentCollections.Domain.DTOs.Remita;

namespace GovernmentCollections.Service.Services.Remita.Transaction;

public interface IRemitaTransactionService
{
    Task<dynamic> ProcessTransactionWithAuthAsync(RemitaTransactionInitiateDto request);
    Task<dynamic> GetTransactionStatusAsync(string transactionId);
}