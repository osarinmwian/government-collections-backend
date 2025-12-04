using GovernmentCollections.Domain.Entities;
using GovernmentCollections.Domain.Enums;

namespace GovernmentCollections.Data.Repositories;

public interface IPaymentRepository
{
    Task<GovernmentPayment> CreateAsync(GovernmentPayment payment);
    Task<GovernmentPayment?> GetByIdAsync(string id);
    Task<GovernmentPayment?> GetByTransactionReferenceAsync(string transactionReference);
    Task<List<GovernmentPayment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
    Task<GovernmentPayment> UpdateAsync(GovernmentPayment payment);
    Task<List<GovernmentPayment>> GetByStatusAsync(TransactionStatus status);
    Task<bool> ExistsAsync(string transactionReference);
}