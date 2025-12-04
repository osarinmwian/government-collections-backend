using KeyLoyalty.Domain.Entities;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Repositories
{
    public interface ICustomerLoyaltyRepository
    {
        Task<CustomerLoyalty?> GetCustomerByUserIdAsync(string userId);
        Task UpdateCustomerAsync(CustomerLoyalty customer);
        Task CreateCustomerAsync(CustomerLoyalty customer);
        
        // Point Transactions
        Task<List<PointTransaction>> GetPointTransactionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 20, int pageNumber = 1);
        Task CreatePointTransactionAsync(PointTransaction transaction);
        Task<List<PointTransaction>> GetExpiringPointsAsync(string userId, DateTime expiryDate);
        Task ExpirePointsAsync(List<string> transactionIds);
        
        // Point Redemptions
        Task<List<PointRedemption>> GetPointRedemptionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 20, int pageNumber = 1);
        Task CreatePointRedemptionAsync(PointRedemption redemption);
        
        // Alerts
        Task<List<LoyaltyAlert>> GetAlertsAsync(string userId, bool unreadOnly = false);
        Task CreateAlertAsync(LoyaltyAlert alert);
        Task MarkAlertAsReadAsync(string alertId);
        Task MarkAllAlertsAsReadAsync(string userId);
        
        // Fraud Detection
        Task<List<PointTransaction>> GetRecentTransactionsAsync(string userId, TimeSpan timeSpan);
        Task<List<PointRedemption>> GetRecentRedemptionsAsync(string userId, TimeSpan timeSpan);
        
        // Point Expiry
        Task<List<CustomerLoyalty>> GetCustomersWithExpiredPointsAsync(DateTime currentDate);
        Task<List<CustomerLoyalty>> GetCustomersWithPointsExpiringOnAsync(DateTime targetDate);
        
        // Admin Operations
        Task<List<CustomerLoyalty>> GetAllCustomersAsync();
    }
}