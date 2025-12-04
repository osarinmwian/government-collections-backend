using KeyLoyalty.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeyLoyalty.Application.Services
{
    public interface ILoyaltyApplicationService
    {
        Task<LoyaltyDashboard> GetDashboardAsync(string accountNumber);
        Task<LoyaltyDashboard> GetDashboardByUserIdAsync(string userId);
        Task<List<RedemptionOption>> GetRedemptionOptionsAsync();
        Task<RedemptionResponse> RedeemPointsAsync(RedeemPointsRequest request);
        Task<int> AssignPointsAsync(string accountNumber, int points, string transactionType, decimal transactionAmount = 0);
        Task<bool> ResetUserPointsAsync(string userId, int correctPoints = 0);
        Task<int> ResetAllUserPointsAsync();
        Task<int> AssignPointsByUserIdAsync(string userIdOrAccount, int points, string transactionType);
        Task<LoyaltyUsageResponse> CheckLoyaltyUsageAsync(string userIdOrAccount, string transactionReference);
        Task<bool> ResetPointsByUserOrAccountAsync(string userIdOrAccount, int points);
        Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(string userIdOrAccount);
        Task<TransactionConfirmationResponse> ConfirmTransactionAsync(TransactionConfirmationRequest request);
        Task<LoyaltyRedemptionStatusDto> CheckRedemptionCreditStatusAsync(string userIdOrAccount, string transactionId);
        Task<UsePointsResponse> UsePointsForTransactionAsync(UsePointsRequest request);
    }
}