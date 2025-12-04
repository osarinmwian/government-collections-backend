using KeyLoyalty.Domain.Entities;
using KeyLoyalty.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Services
{
    public interface IAlertService
    {
        Task SendPointEarningAlertAsync(string userId, string accountNumber, int points, string transactionType, decimal amount);
        Task SendPointRedemptionAlertAsync(string userId, string accountNumber, int points, decimal amount, string redemptionType);
        Task SendPointExpiryAlertAsync(string userId, string accountNumber, int expiringPoints, DateTime expiryDate);
        Task SendTierUpgradeAlertAsync(string userId, string accountNumber, LoyaltyTier oldTier, LoyaltyTier newTier);
        Task ProcessExpiryRemindersAsync();
        Task SendPointExpiryNotificationAsync(string userId, int expiredPoints);
        Task SendPointExpiryReminderAsync(string userId, int points, int daysUntilExpiry);
    }

    public class AlertService : IAlertService
    {
        private readonly ICustomerLoyaltyRepository _repository;
        private readonly ILogger<AlertService> _logger;

        public AlertService(ICustomerLoyaltyRepository repository, ILogger<AlertService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task SendPointEarningAlertAsync(string userId, string accountNumber, int points, string transactionType, decimal amount)
        {
            try
            {
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AlertType = "EARNING",
                    Message = $"You earned {points} points from {GetTransactionTypeDisplay(transactionType)} transaction of ₦{amount:N2}",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["points"] = points,
                        ["transactionType"] = transactionType,
                        ["amount"] = amount,
                        ["icon"] = GetTransactionIcon(transactionType)
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Point earning alert sent to user {UserId}: {Points} points", userId, points);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending point earning alert to user {UserId}", userId);
            }
        }

        public async Task SendPointRedemptionAlertAsync(string userId, string accountNumber, int points, decimal amount, string redemptionType)
        {
            try
            {
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AlertType = "REDEMPTION",
                    Message = $"You redeemed {points} points for ₦{amount:N2} {GetRedemptionTypeDisplay(redemptionType)}",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["points"] = points,
                        ["amount"] = amount,
                        ["redemptionType"] = redemptionType,
                        ["icon"] = GetRedemptionIcon(redemptionType)
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Point redemption alert sent to user {UserId}: {Points} points redeemed", userId, points);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending point redemption alert to user {UserId}", userId);
            }
        }

        public async Task SendPointExpiryAlertAsync(string userId, string accountNumber, int expiringPoints, DateTime expiryDate)
        {
            try
            {
                var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AlertType = "EXPIRY",
                    Message = $"⚠️ {expiringPoints} points will expire in {daysUntilExpiry} days on {expiryDate:MMM dd, yyyy}. Use them before they expire!",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["expiringPoints"] = expiringPoints,
                        ["expiryDate"] = expiryDate,
                        ["daysUntilExpiry"] = daysUntilExpiry,
                        ["icon"] = "⏰"
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Point expiry alert sent to user {UserId}: {Points} points expiring", userId, expiringPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending point expiry alert to user {UserId}", userId);
            }
        }

        public async Task SendTierUpgradeAlertAsync(string userId, string accountNumber, LoyaltyTier oldTier, LoyaltyTier newTier)
        {
            try
            {
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AlertType = "TIER_UPGRADE",
                    Message = $"🎉 Congratulations! You've been upgraded from {oldTier} to {newTier} tier. Enjoy enhanced benefits!",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["oldTier"] = oldTier.ToString(),
                        ["newTier"] = newTier.ToString(),
                        ["oldTierIcon"] = GetTierIcon(oldTier),
                        ["newTierIcon"] = GetTierIcon(newTier),
                        ["icon"] = "🎉"
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Tier upgrade alert sent to user {UserId}: {OldTier} -> {NewTier}", userId, oldTier, newTier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending tier upgrade alert to user {UserId}", userId);
            }
        }

        public Task ProcessExpiryRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Processing point expiry reminders");
                
                var reminderDates = new[]
                {
                    DateTime.UtcNow.AddDays(30),
                    DateTime.UtcNow.AddDays(7),
                    DateTime.UtcNow.AddDays(1)
                };

                foreach (var reminderDate in reminderDates)
                {
                    _logger.LogInformation("Would process expiry reminders for date: {ReminderDate}", reminderDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expiry reminders");
            }
            return Task.CompletedTask;
        }
        
        public async Task SendPointExpiryNotificationAsync(string userId, int expiredPoints)
        {
            try
            {
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AlertType = "EXPIRED",
                    Message = $"❌ {expiredPoints} loyalty points have expired and been removed from your account. Earn more points to maintain your balance!",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["expiredPoints"] = expiredPoints,
                        ["icon"] = "❌"
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Point expiry notification sent to user {UserId}: {Points} points expired", userId, expiredPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending point expiry notification to user {UserId}", userId);
            }
        }
        
        public async Task SendPointExpiryReminderAsync(string userId, int points, int daysUntilExpiry)
        {
            try
            {
                var urgencyIcon = daysUntilExpiry switch
                {
                    1 => "🚨",
                    <= 7 => "⚠️",
                    _ => "⏰"
                };
                
                var alert = new LoyaltyAlert
                {
                    UserId = userId,
                    AlertType = "EXPIRY_REMINDER",
                    Message = $"{urgencyIcon} Reminder: {points} loyalty points will expire in {daysUntilExpiry} day{(daysUntilExpiry == 1 ? "" : "s")}. Use them now!",
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["points"] = points,
                        ["daysUntilExpiry"] = daysUntilExpiry,
                        ["icon"] = urgencyIcon
                    }
                };

                await _repository.CreateAlertAsync(alert);
                _logger.LogInformation("Point expiry reminder sent to user {UserId}: {Points} points expiring in {Days} days", userId, points, daysUntilExpiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending point expiry reminder to user {UserId}", userId);
            }
        }

        private string GetTransactionTypeDisplay(string transactionType)
        {
            return transactionType.ToUpper() switch
            {
                "AIRTIME" => "airtime purchase",
                "BILL_PAYMENT" => "bill payment",
                "TRANSFER" => "money transfer",
                "NIP_TRANSFER" => "bank transfer",
                "DEPOSIT" => "account deposit",
                _ => "transaction"
            };
        }

        private string GetRedemptionTypeDisplay(string redemptionType)
        {
            return redemptionType.ToUpper() switch
            {
                "CASHBACK" => "cashback",
                "DISCOUNT" => "discount",
                "VOUCHER" => "voucher",
                "TRANSFER" => "account credit",
                _ => "redemption"
            };
        }

        private string GetTransactionIcon(string transactionType)
        {
            return transactionType.ToUpper() switch
            {
                "AIRTIME" => "📱",
                "BILL_PAYMENT" => "💡",
                "TRANSFER" => "💸",
                "NIP_TRANSFER" => "🏦",
                "DEPOSIT" => "💰",
                _ => "💳"
            };
        }

        private string GetRedemptionIcon(string redemptionType)
        {
            return redemptionType.ToUpper() switch
            {
                "CASHBACK" => "💰",
                "DISCOUNT" => "🏷️",
                "VOUCHER" => "🎫",
                "TRANSFER" => "💸",
                _ => "🎁"
            };
        }

        private string GetTierIcon(LoyaltyTier tier)
        {
            return tier switch
            {
                LoyaltyTier.Bronze => "🥉",
                LoyaltyTier.Silver => "🥈",
                LoyaltyTier.Gold => "🥇",
                LoyaltyTier.Diamond => "💎",
                LoyaltyTier.Platinum => "💍",
                _ => "🥉"
            };
        }
    }
}