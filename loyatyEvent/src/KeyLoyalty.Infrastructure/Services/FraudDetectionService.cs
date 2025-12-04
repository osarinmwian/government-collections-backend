using KeyLoyalty.Domain.Entities;
using KeyLoyalty.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Services
{
    public class FraudDetectionResult
    {
        public bool IsSuspicious { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public List<string> Flags { get; set; } = new();
    }

    public interface IFraudDetectionService
    {
        Task<FraudDetectionResult> ValidatePointEarningAsync(string userId, string accountNumber, int points, string transactionType, decimal amount);
        Task<FraudDetectionResult> ValidatePointRedemptionAsync(string userId, string accountNumber, int pointsToRedeem, string redemptionType);
    }

    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly ICustomerLoyaltyRepository _repository;
        private readonly ILogger<FraudDetectionService> _logger;

        // Fraud detection thresholds
        private const int MAX_DAILY_POINTS = 100;
        private const int MAX_HOURLY_POINTS = 20;
        private const int MAX_DAILY_REDEMPTIONS = 5;
        private const decimal MAX_DAILY_REDEMPTION_AMOUNT = 50000m;
        private const int SUSPICIOUS_VELOCITY_THRESHOLD = 10; // transactions per hour

        public FraudDetectionService(ICustomerLoyaltyRepository repository, ILogger<FraudDetectionService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<FraudDetectionResult> ValidatePointEarningAsync(string userId, string accountNumber, int points, string transactionType, decimal amount)
        {
            var result = new FraudDetectionResult();
            var flags = new List<string>();

            try
            {
                // Check daily point earning limits
                var dailyTransactions = await _repository.GetRecentTransactionsAsync(userId, TimeSpan.FromDays(1));
                var dailyPoints = dailyTransactions.Where(t => t.Points > 0).Sum(t => t.Points);
                
                if (dailyPoints + points > MAX_DAILY_POINTS)
                {
                    flags.Add($"Daily point limit exceeded: {dailyPoints + points}/{MAX_DAILY_POINTS}");
                    result.RiskLevel = "HIGH";
                }

                // Check hourly point earning limits
                var hourlyTransactions = await _repository.GetRecentTransactionsAsync(userId, TimeSpan.FromHours(1));
                var hourlyPoints = hourlyTransactions.Where(t => t.Points > 0).Sum(t => t.Points);
                
                if (hourlyPoints + points > MAX_HOURLY_POINTS)
                {
                    flags.Add($"Hourly point limit exceeded: {hourlyPoints + points}/{MAX_HOURLY_POINTS}");
                    result.RiskLevel = "HIGH";
                }

                // Check transaction velocity
                if (hourlyTransactions.Count >= SUSPICIOUS_VELOCITY_THRESHOLD)
                {
                    flags.Add($"High transaction velocity: {hourlyTransactions.Count} transactions in last hour");
                    result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "MEDIUM" : result.RiskLevel;
                }

                // Check for unusual transaction patterns
                var recentTypes = hourlyTransactions.GroupBy(t => t.TransactionType).Select(g => new { Type = g.Key, Count = g.Count() });
                foreach (var typeGroup in recentTypes)
                {
                    if (typeGroup.Count > 5) // More than 5 of same transaction type in an hour
                    {
                        flags.Add($"Repetitive transaction pattern: {typeGroup.Count} {typeGroup.Type} transactions");
                        result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "MEDIUM" : result.RiskLevel;
                    }
                }

                // Check for round number amounts (potential testing)
                if (amount > 0 && amount % 1000 == 0 && amount >= 10000)
                {
                    flags.Add($"Round number transaction amount: ₦{amount:N2}");
                    result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "LOW" : result.RiskLevel;
                }

                result.IsSuspicious = flags.Any();
                result.Flags = flags;
                result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "LOW" : result.RiskLevel;
                result.Reason = flags.Any() ? string.Join("; ", flags) : "No suspicious activity detected";

                if (result.IsSuspicious)
                {
                    _logger.LogWarning("Suspicious point earning detected for user {UserId}: {Reason}", userId, result.Reason);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fraud detection for point earning - User: {UserId}", userId);
                return new FraudDetectionResult
                {
                    IsSuspicious = false,
                    RiskLevel = "LOW",
                    Reason = "Fraud detection unavailable"
                };
            }
        }

        public async Task<FraudDetectionResult> ValidatePointRedemptionAsync(string userId, string accountNumber, int pointsToRedeem, string redemptionType)
        {
            var result = new FraudDetectionResult();
            var flags = new List<string>();

            try
            {
                // Check daily redemption limits
                var dailyRedemptions = await _repository.GetRecentRedemptionsAsync(userId, TimeSpan.FromDays(1));
                var dailyRedemptionCount = dailyRedemptions.Count;
                var dailyRedemptionAmount = dailyRedemptions.Sum(r => r.AmountRedeemed);

                if (dailyRedemptionCount >= MAX_DAILY_REDEMPTIONS)
                {
                    flags.Add($"Daily redemption count limit exceeded: {dailyRedemptionCount}/{MAX_DAILY_REDEMPTIONS}");
                    result.RiskLevel = "HIGH";
                }

                var estimatedAmount = pointsToRedeem * 1.0m; // Assuming 1 point = ₦1
                if (dailyRedemptionAmount + estimatedAmount > MAX_DAILY_REDEMPTION_AMOUNT)
                {
                    flags.Add($"Daily redemption amount limit exceeded: ₦{dailyRedemptionAmount + estimatedAmount:N2}/₦{MAX_DAILY_REDEMPTION_AMOUNT:N2}");
                    result.RiskLevel = "HIGH";
                }

                // Check for large single redemptions
                if (pointsToRedeem > 10000)
                {
                    flags.Add($"Large single redemption: {pointsToRedeem} points");
                    result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "MEDIUM" : result.RiskLevel;
                }

                // Check redemption velocity
                var hourlyRedemptions = await _repository.GetRecentRedemptionsAsync(userId, TimeSpan.FromHours(1));
                if (hourlyRedemptions.Count >= 3)
                {
                    flags.Add($"High redemption velocity: {hourlyRedemptions.Count} redemptions in last hour");
                    result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "MEDIUM" : result.RiskLevel;
                }

                // Check for immediate redemption after earning
                var recentTransactions = await _repository.GetRecentTransactionsAsync(userId, TimeSpan.FromMinutes(5));
                if (recentTransactions.Any(t => t.Points > 0))
                {
                    flags.Add("Immediate redemption after point earning");
                    result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "LOW" : result.RiskLevel;
                }

                result.IsSuspicious = flags.Any();
                result.Flags = flags;
                result.RiskLevel = string.IsNullOrEmpty(result.RiskLevel) ? "LOW" : result.RiskLevel;
                result.Reason = flags.Any() ? string.Join("; ", flags) : "No suspicious activity detected";

                if (result.IsSuspicious)
                {
                    _logger.LogWarning("Suspicious point redemption detected for user {UserId}: {Reason}", userId, result.Reason);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fraud detection for point redemption - User: {UserId}", userId);
                return new FraudDetectionResult
                {
                    IsSuspicious = false,
                    RiskLevel = "LOW",
                    Reason = "Fraud detection unavailable"
                };
            }
        }
    }
}