using KeyLoyalty.Application.DTOs;
using KeyLoyalty.Domain.Entities;
using KeyLoyalty.Domain.Events;
using KeyLoyalty.Infrastructure.Repositories;
using KeyLoyalty.Infrastructure.Events;
using KeyLoyalty.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeyLoyalty.Application.Services
{
    public class LoyaltyApplicationService : ILoyaltyApplicationService
    {
        private readonly ICustomerLoyaltyRepository _repository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<LoyaltyApplicationService> _logger;
        private readonly IAccountMappingService _accountMapping;
        private readonly IAccountingService _accountingService;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public LoyaltyApplicationService(ICustomerLoyaltyRepository repository, IEventPublisher eventPublisher, ILogger<LoyaltyApplicationService> logger, IAccountMappingService accountMapping, IAccountingService accountingService, IPaymentService paymentService, IConfiguration configuration)
        {
            _repository = repository;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _accountMapping = accountMapping;
            _accountingService = accountingService;
            _paymentService = paymentService;
            _configuration = configuration;
        }

        public async Task<LoyaltyDashboard> GetDashboardAsync(string accountNumber)
        {
            _logger.LogInformation("Getting dashboard for account: {AccountNumber}", accountNumber);
            
            if (!IsValidAccountNumber(accountNumber))
            {
                throw new ArgumentException("Invalid account number. Must be exactly 10 digits.");
            }

            var userId = await _accountMapping.GetUserIdByAccountNumberAsync(accountNumber);
            if (string.IsNullOrEmpty(userId))
            {
                throw new KeyNotFoundException($"Account number {accountNumber} not found or not mapped to a user.");
            }
                
            return await GetDashboardByUserIdAsync(userId);
        }

        public async Task<LoyaltyDashboard> GetDashboardByUserIdAsync(string userId)
        {
            _logger.LogInformation("Getting dashboard for user: {UserId}", userId);
            
            // If userId doesn't look like an account number, try to resolve it
            var actualUserId = userId;
            if (!IsValidAccountNumber(userId))
            {
                var resolvedUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userId);
                if (string.IsNullOrEmpty(resolvedUserId))
                {
                    throw new KeyNotFoundException($"User {userId} not found in the loyalty program.");
                }
                actualUserId = resolvedUserId;
                _logger.LogInformation("Resolved user {OriginalUserId} to account {ActualUserId}", userId, actualUserId);
            }
            
            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"User {userId} not found in the loyalty program.");
            }
            
            // Get associated account numbers
            var accountNumbers = await _accountMapping.GetAccountNumbersByUserIdAsync(actualUserId);
            
            _logger.LogInformation("Dashboard data retrieved from database - User: {UserId}, Points: {Points}, Tier: {Tier}, Accounts: {AccountCount}", 
                actualUserId, customer.TotalPoints, customer.Tier, accountNumbers.Count);
            
            return new LoyaltyDashboard
            {
                UserId = actualUserId,
                AccountNumbers = accountNumbers,
                TotalPoints = customer.TotalPoints,
                Tier = customer.Tier.ToString(),
                TierIcon = GetTierIcon(customer.Tier),
                PointsToNextTier = CalculatePointsToNextTier(customer.TotalPoints),
                PointsExpiryDate = customer.PointsExpiryDate,
                EarningPoints = GetEarningPoints(),
                TierPoints = GetTierPoints(customer.Tier)
            };
        }

        public Task<List<RedemptionOption>> GetRedemptionOptionsAsync()
        {
            var options = new List<RedemptionOption>
            {
                new() { Type = "Send Money", Description = "Send money to beneficiaries", Icon = "💸" },
                new() { Type = "Airtime/Data Purchase", Description = "Buy airtime and data", Icon = "📱" },
                new() { Type = "Bill Payments", Description = "Pay utility bills", Icon = "💡" }
            };
            return Task.FromResult(options);
        }

        public async Task<RedemptionResponse> RedeemPointsAsync(RedeemPointsRequest request)
        {
            _logger.LogInformation("Processing redemption request - Account: {AccountNumber}, Points: {Points}, Type: {Type}", 
                request.AccountNumber, request.PointsToRedeem, request.RedemptionType);
            
            if (!IsValidAccountNumber(request.AccountNumber))
            {
                _logger.LogWarning("Invalid account number for redemption: {AccountNumber}", request.AccountNumber);
                throw new ArgumentException("Invalid account number. Must be exactly 10 digits.");
            }

            var userId = await _accountMapping.GetUserIdByAccountNumberAsync(request.AccountNumber);
            if (string.IsNullOrEmpty(userId))
            {
                throw new KeyNotFoundException($"Account number {request.AccountNumber} not found or not mapped to a user.");
            }

            var customer = await _repository.GetCustomerByUserIdAsync(userId);
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for redemption: {AccountNumber}", request.AccountNumber);
                throw new KeyNotFoundException($"User {request.Username} not found in the loyalty program.");
            }

            _logger.LogInformation("Customer data retrieved from database - Account: {AccountNumber}, Current Points: {Points}", 
                request.AccountNumber, customer.TotalPoints);

            if (customer.TotalPoints < request.PointsToRedeem)
            {
                _logger.LogWarning("Insufficient points for redemption - Account: {AccountNumber}, Available: {Available}, Requested: {Requested}", 
                    request.AccountNumber, customer.TotalPoints, request.PointsToRedeem);
                return new RedemptionResponse
                {
                    Success = false,
                    Message = "Insufficient points for redemption",
                    RemainingPoints = customer.TotalPoints,
                    AmountRedeemed = 0
                };
            }

            // Calculate redemption value based on type
            var pointValue = GetPointValue(request.RedemptionType);
            var amountRedeemed = request.PointsToRedeem * pointValue;
            var oldPoints = customer.TotalPoints;
            var oldTier = customer.Tier;
            
            customer.TotalPoints -= request.PointsToRedeem;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            _logger.LogInformation("Updating customer in database - Account: {AccountNumber}, Points: {OldPoints} -> {NewPoints}, Tier: {OldTier} -> {NewTier}", 
                request.AccountNumber, oldPoints, customer.TotalPoints, oldTier, customer.Tier);
            
            await _repository.UpdateCustomerAsync(customer);
            
            var transactionId = $"RED_{DateTime.Now.Ticks}";
            
            // Handle different redemption types
            var success = true;
            var message = "";
            
            try
            {
                switch (request.RedemptionType?.ToUpper())
                {
                    case "TRANSFER":
                    case "NIP":
                    case null:
                    case "":
                        await _paymentService.CreditCustomerAccountAsync(request.AccountNumber, amountRedeemed, transactionId);
                        message = $"₦{amountRedeemed:N2} has been credited to your account";
                        _logger.LogInformation("Account credited - Account: {Account}, Amount: ₦{Amount}", request.AccountNumber, amountRedeemed);
                        break;
                        
                    case "AIRTIME":
                        message = $"₦{amountRedeemed:N2} available for airtime purchase";
                        _logger.LogInformation("Airtime redemption - Account: {Account}, Amount: ₦{Amount}", request.AccountNumber, amountRedeemed);
                        break;
                        
                    case "BILL_PAYMENT":
                        message = $"₦{amountRedeemed:N2} available for bill payment";
                        _logger.LogInformation("Bill payment redemption - Account: {Account}, Amount: ₦{Amount}", request.AccountNumber, amountRedeemed);
                        break;
                        
                    default:
                        message = $"₦{amountRedeemed:N2} redeemed successfully";
                        break;
                }
            }
            catch (Exception ex)
            {
                // Rollback points if processing fails
                customer.TotalPoints = oldPoints;
                customer.Tier = oldTier;
                await _repository.UpdateCustomerAsync(customer);
                
                _logger.LogError(ex, "Redemption processing failed - Account: {AccountNumber}, rolling back points", request.AccountNumber);
                return new RedemptionResponse
                {
                    Success = false,
                    Message = "Redemption processing failed. Points have been restored.",
                    RemainingPoints = customer.TotalPoints,
                    AmountRedeemed = 0
                };
            }
            
            // Process accounting entries with retry logic
            try
            {
                var accountingTransactionId = await _accountingService.ProcessRedemptionAccountingAsync(
                    request.AccountNumber, 
                    request.PointsToRedeem, 
                    amountRedeemed, 
                    request.RedemptionType ?? "CASH");
                _logger.LogInformation("Accounting transaction {AccountingTxnId} completed for redemption {TransactionId}", 
                    accountingTransactionId, transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical: Accounting entry failed after retries for redemption - Account: {AccountNumber}, Transaction: {TransactionId}", 
                    request.AccountNumber, transactionId);
                
                // For critical accounting failures, consider rolling back the redemption
                // This is a business decision - you may want to allow redemption to proceed
                // but flag for manual accounting reconciliation
                _logger.LogWarning("Redemption {TransactionId} completed but accounting failed - manual reconciliation required", transactionId);
            }
            
            _logger.LogInformation("Redemption completed successfully - Account: {AccountNumber}, Type: {Type}, Amount: ₦{Amount}, Remaining Points: {Points}", 
                request.AccountNumber, request.RedemptionType, amountRedeemed, customer.TotalPoints);
            
            return new RedemptionResponse
            {
                Success = success,
                Message = message,
                AmountRedeemed = amountRedeemed,
                RemainingPoints = customer.TotalPoints,
                TransactionId = transactionId
            };
        }
        
        private decimal GetPointValue(string? redemptionType)
        {
            return 1.0m; // ₦1.00 per point for all redemption types
        }

        public async Task<int> AssignPointsAsync(string accountNumber, int points, string transactionType, decimal transactionAmount = 0)
        {
            _logger.LogInformation("Assigning {Points} points to account {AccountNumber} for {TransactionType} (Amount: {Amount})", 
                points, accountNumber, transactionType, transactionAmount);
            
            if (!IsValidAccountNumber(accountNumber))
            {
                throw new ArgumentException("Invalid account number. Must be exactly 10 digits.");
            }

            var userId = await _accountMapping.GetUserIdByAccountNumberAsync(accountNumber);
            if (string.IsNullOrEmpty(userId))
            {
                throw new KeyNotFoundException($"Account number {accountNumber} not found or not mapped to a user.");
            }

            var customer = await _repository.GetCustomerByUserIdAsync(userId);
            if (customer == null)
            {
                customer = new CustomerLoyalty
                {
                    UserId = userId,
                    TotalPoints = 0,
                    Tier = LoyaltyTier.Bronze,
                    LastUpdated = DateTime.UtcNow,
                    PointsExpiryDate = DateTime.UtcNow.AddYears(1)
                };
            }
                
            // Handle clear points operation
            if (transactionType == "CLEAR_POINTS")
            {
                points = -customer.TotalPoints; // Reset to 0
            }
            // Calculate points based on transaction type and amount
            else if (transactionAmount > 0)
            {
                points = CalculatePointsForTransaction(transactionType, transactionAmount);
                
                if (points == 0)
                {
                    _logger.LogInformation("No points assigned - {TransactionType} amount {Amount} below minimum threshold or invalid type", transactionType, transactionAmount);
                    return 0;
                }
                
                _logger.LogInformation("Calculated {Points} points for {TransactionType} transaction (Amount: {Amount})", points, transactionType, transactionAmount);
            }

            customer.TotalPoints += points;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            // Extend expiry date when points are added (not for clear points)
            if (transactionType != "CLEAR_POINTS" && points > 0)
            {
                customer.PointsExpiryDate = DateTime.UtcNow.AddYears(1);
            }
            
            await _repository.UpdateCustomerAsync(customer);
            
            _logger.LogInformation("Successfully assigned {Points} points to {AccountNumber}. New total: {TotalPoints}", 
                points, accountNumber, customer.TotalPoints);
            
            return points;
        }

        private List<EarningPoint> GetEarningPoints()
        {
            return new List<EarningPoint>
            {
                new() { Type = "Airtime/Data Purchases", Points = 1, Icon = "📱" },
                new() { Type = "Transfer", Points = 2, Icon = "💸" },
                new() { Type = "Bill Payments", Points = 3, Icon = "💡" }
            };
        }

        private List<TierPoint> GetTierPoints(LoyaltyTier currentTier)
        {
            return new List<TierPoint>
            {
                new() { Tier = "Bronze", TierNumber = "Tier1", Range = "0 - 500", Icon = "🥉", IsActive = currentTier == LoyaltyTier.Bronze },
                new() { Tier = "Silver", TierNumber = "Tier2", Range = "501 - 3,000", Icon = "🥈", IsActive = currentTier == LoyaltyTier.Silver },
                new() { Tier = "Gold", TierNumber = "Tier3", Range = "3,001 - 6,000", Icon = "🥇", IsActive = currentTier == LoyaltyTier.Gold },
                new() { Tier = "Platinum", TierNumber = "Tier4", Range = "6,001 - 10,000", Icon = "💎", IsActive = currentTier == LoyaltyTier.Platinum },
                new() { Tier = "Diamond", TierNumber = "Tier5", Range = "10,001 - 50,000", Icon = "💍", IsActive = currentTier == LoyaltyTier.Diamond }
            };
        }

        private string GetTierIcon(LoyaltyTier tier)
        {
            return tier switch
            {
                LoyaltyTier.Bronze => "🥉",
                LoyaltyTier.Silver => "🥈",
                LoyaltyTier.Gold => "🥇",
                LoyaltyTier.Platinum => "💎",
                LoyaltyTier.Diamond => "💍",
                _ => "🥉"
            };
        }

        private int CalculatePointsToNextTier(int points)
        {
            return points switch
            {
                < 501 => 501 - points,
                < 3001 => 3001 - points,
                < 6001 => 6001 - points,
                < 10001 => 10001 - points,
                _ => 0
            };
        }

        private LoyaltyTier CalculateTier(int points)
        {
            return points switch
            {
                >= 10001 => LoyaltyTier.Diamond,
                >= 6001 => LoyaltyTier.Platinum,
                >= 3001 => LoyaltyTier.Gold,
                >= 501 => LoyaltyTier.Silver,
                _ => LoyaltyTier.Bronze
            };
        }

        public async Task<bool> ResetUserPointsAsync(string userId, int correctPoints = 0)
        {
            _logger.LogWarning("Resetting points for user {UserId} to {Points}", userId, correctPoints);
            
            var customer = await _repository.GetCustomerByUserIdAsync(userId);
            if (customer == null)
            {
                _logger.LogWarning("User {UserId} not found for points reset", userId);
                return false;
            }
            
            var oldPoints = customer.TotalPoints;
            customer.TotalPoints = correctPoints;
            customer.Tier = CalculateTier(correctPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            await _repository.UpdateCustomerAsync(customer);
            
            _logger.LogWarning("Points reset completed - User: {UserId}, Points: {OldPoints} -> {NewPoints}", 
                userId, oldPoints, correctPoints);
            
            return true;
        }

        public async Task<int> ResetAllUserPointsAsync()
        {
            _logger.LogWarning("Resetting ALL user points to fix duplicate awards");
            
            var customers = await _repository.GetAllCustomersAsync();
            var resetCount = 0;
            
            foreach (var customer in customers)
            {
                var oldPoints = customer.TotalPoints;
                customer.TotalPoints = 0;
                customer.Tier = LoyaltyTier.Bronze;
                customer.LastUpdated = DateTime.UtcNow;
                
                await _repository.UpdateCustomerAsync(customer);
                
                _logger.LogInformation("Reset user {UserId}: {OldPoints} -> 0 points", customer.UserId, oldPoints);
                resetCount++;
            }
            
            _logger.LogWarning("Reset completed for {Count} users", resetCount);
            return resetCount;
        }

        public async Task<int> AssignPointsByUserIdAsync(string userIdOrAccount, int points, string transactionType)
        {
            _logger.LogInformation("Assigning {Points} points to {UserIdOrAccount} for {TransactionType}", points, userIdOrAccount, transactionType);
            
            string actualUserId;
            
            // Check if input is account number (10 digits) or username
            if (IsValidAccountNumber(userIdOrAccount))
            {
                // It's an account number, resolve to userId
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {userIdOrAccount} not found.");
                }
                _logger.LogInformation("Resolved account {Account} to userId {UserId}", userIdOrAccount, actualUserId);
            }
            else
            {
                // It's a username, validate it exists in account mapping
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Username {userIdOrAccount} not found.");
                }
                _logger.LogInformation("Validated username {Username} maps to userId {UserId}", userIdOrAccount, actualUserId);
            }
            
            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                customer = new CustomerLoyalty
                {
                    UserId = actualUserId,
                    TotalPoints = 0,
                    Tier = LoyaltyTier.Bronze,
                    LastUpdated = DateTime.UtcNow,
                    PointsExpiryDate = DateTime.UtcNow.AddYears(1)
                };
            }
            
            customer.TotalPoints += points;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            if (points > 0)
            {
                customer.PointsExpiryDate = DateTime.UtcNow.AddYears(1);
            }
            
            await _repository.UpdateCustomerAsync(customer);
            
            _logger.LogInformation("Successfully assigned {Points} points to {Input} (userId: {UserId}). New total: {TotalPoints}", 
                points, userIdOrAccount, actualUserId, customer.TotalPoints);
            return points;
        }

        public async Task<LoyaltyUsageResponse> CheckLoyaltyUsageAsync(string userIdOrAccount, string transactionReference)
        {
            _logger.LogInformation("Checking loyalty usage for {User} transaction {TransactionReference}", userIdOrAccount, transactionReference);
            
            // Resolve user to account number
            string actualUserId;
            if (IsValidAccountNumber(userIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {userIdOrAccount} not found.");
                }
            }
            else
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Username {userIdOrAccount} not found.");
                }
            }

            var response = new LoyaltyUsageResponse
            {
                TransactionReference = transactionReference,
                AccountNumber = actualUserId
            };

            try
            {
                // Check OmniChannel transaction for this user
                var omniTransaction = await GetOmniChannelTransactionForUserAsync(actualUserId, transactionReference);
                
                if (omniTransaction == null)
                {
                    response.UsedLoyaltyPoints = false;
                    response.Message = "Transaction not done using loyalty points";
                    return response;
                }

                // Check if transaction used loyalty points
                var usedLoyaltyPoints = CheckIfUsedLoyaltyPoints(omniTransaction);
                
                if (!usedLoyaltyPoints)
                {
                    response.UsedLoyaltyPoints = false;
                    response.Message = "Transaction not done using loyalty points";
                    return response;
                }

                // Transaction used loyalty points
                response.UsedLoyaltyPoints = true;
                var pointsUsed = ExtractPointsFromMemo(omniTransaction.Memo);
                response.PointsUsed = pointsUsed;
                response.PointsValue = pointsUsed * 1.0m;

                if (omniTransaction.Status == "00") // Success - deduct points
                {
                    var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
                    if (customer != null)
                    {
                        customer.TotalPoints -= pointsUsed;
                        customer.Tier = CalculateTier(customer.TotalPoints);
                        customer.LastUpdated = DateTime.UtcNow;
                        await _repository.UpdateCustomerAsync(customer);
                        
                        response.RemainingPoints = customer.TotalPoints;
                        response.Message = $"Transaction completed using {pointsUsed} loyalty points. ₦{response.PointsValue:N2} deducted. Remaining points: {customer.TotalPoints}";
                    }
                }
                else
                {
                    response.Message = $"Transaction used {pointsUsed} loyalty points but failed. No points deducted.";
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking loyalty usage for {User} transaction {TransactionReference}", userIdOrAccount, transactionReference);
                throw;
            }
        }

        private async Task<OmniChannelTransaction?> GetOmniChannelTransactionForUserAsync(string userId, string transactionReference)
        {
            // Query OmniChannel database for transaction by user and reference
            // SELECT * FROM KeystoneOmniTransactions WHERE Draccount = @userId AND Requestid = @transactionReference
            await Task.Delay(100); // Simulate database call
            
            // Return mock transaction for demonstration
            return new OmniChannelTransaction
            {
                TransactionReference = transactionReference,
                DebitAccount = userId,
                Amount = 500m,
                Status = "00", // Success
                TransactionType = "Transfer",
                Memo = "UseLoyaltyPoints:300" // Custom field indicating loyalty points used
            };
        }

        private bool CheckIfUsedLoyaltyPoints(OmniChannelTransaction transaction)
        {
            // Check if transaction memo or custom field indicates loyalty points were used
            return !string.IsNullOrEmpty(transaction.Memo) && transaction.Memo.Contains("UseLoyaltyPoints");
        }

        private async Task<int> DeductLoyaltyPointsAsync(OmniChannelTransaction transaction)
        {
            try
            {
                // Extract points from memo field
                var pointsToDeduct = ExtractPointsFromMemo(transaction.Memo);
                if (pointsToDeduct <= 0) return 0;

                var userId = await _accountMapping.GetUserIdByAccountNumberAsync(transaction.DebitAccount);
                if (string.IsNullOrEmpty(userId)) return 0;

                var customer = await _repository.GetCustomerByUserIdAsync(userId);
                if (customer == null || customer.TotalPoints < pointsToDeduct) return 0;

                // Deduct points
                customer.TotalPoints -= pointsToDeduct;
                customer.Tier = CalculateTier(customer.TotalPoints);
                customer.LastUpdated = DateTime.UtcNow;
                
                await _repository.UpdateCustomerAsync(customer);
                
                _logger.LogInformation("Deducted {Points} loyalty points from user {UserId} for transaction {TxnRef}", 
                    pointsToDeduct, userId, transaction.TransactionReference);
                
                return pointsToDeduct;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting loyalty points for transaction {TxnRef}", transaction.TransactionReference);
                return 0;
            }
        }

        private int ExtractPointsFromMemo(string? memo)
        {
            if (string.IsNullOrEmpty(memo)) return 0;
            
            // Extract points from memo like "UseLoyaltyPoints:300"
            var parts = memo.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out var points))
            {
                return points;
            }
            return 0;
        }

        public class OmniChannelTransaction
        {
            public string TransactionReference { get; set; } = string.Empty;
            public string DebitAccount { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Status { get; set; } = string.Empty;
            public string TransactionType { get; set; } = string.Empty;
            public string? Memo { get; set; }
        }

        public async Task<TransactionConfirmationResponse> ConfirmTransactionAsync(TransactionConfirmationRequest request)
        {
            _logger.LogInformation("Processing transaction confirmation - RedemptionId: {RedemptionId}, Success: {Success}", 
                request.RedemptionId, request.IsSuccessful);

            if (!request.IsSuccessful)
            {
                _logger.LogWarning("Transaction failed, rolling back points - RedemptionId: {RedemptionId}, Reason: {Reason}", 
                    request.RedemptionId, request.FailureReason);
                
                return new TransactionConfirmationResponse
                {
                    Success = true,
                    Message = "Points have been rolled back due to transaction failure",
                    PointsRolledBack = true
                };
            }

            _logger.LogInformation("Transaction confirmed successful - RedemptionId: {RedemptionId}", request.RedemptionId);
            
            return new TransactionConfirmationResponse
            {
                Success = true,
                Message = "Transaction confirmed successfully",
                PointsRolledBack = false
            };
        }

        public async Task<bool> ClearPointsByUserOrAccountAsync(string userIdOrAccount)
        {
            _logger.LogWarning("Resetting points for user {UserIdOrAccount} to 0", userIdOrAccount);
            
            // Resolve user to userId
            string actualUserId;
            if (IsValidAccountNumber(userIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    _logger.LogWarning("Account number {UserIdOrAccount} not found for points reset", userIdOrAccount);
                    return false;
                }
            }
            else
            {
                // If it's not an account number, treat it as a username/userId
                actualUserId = userIdOrAccount;
            }
            
            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                customer = new CustomerLoyalty
                {
                    UserId = actualUserId,
                    TotalPoints = 0,
                    Tier = CalculateTier(0),
                    LastUpdated = DateTime.UtcNow,
                    PointsExpiryDate = DateTime.UtcNow.AddYears(1)
                };
                _logger.LogInformation("Created new customer {UserIdOrAccount} with 0 points", userIdOrAccount);
            }
            else
            {
                var oldPoints = customer.TotalPoints;
                customer.TotalPoints = 0;
                customer.Tier = CalculateTier(0);
                customer.LastUpdated = DateTime.UtcNow;
                _logger.LogInformation("Points cleared for {UserIdOrAccount}: {OldPoints} -> 0", 
                    userIdOrAccount, oldPoints);
            }
            
            await _repository.UpdateCustomerAsync(customer);
            
            return true;
        }

        public async Task<bool> ResetPointsByUserOrAccountAsync(string userIdOrAccount, int points)
        {
            _logger.LogInformation("Resetting points to {Points} for {UserIdOrAccount}", points, userIdOrAccount);
            
            // Resolve user to userId
            string actualUserId;
            if (IsValidAccountNumber(userIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {userIdOrAccount} not found.");
                }
            }
            else
            {
                // If it's not an account number, treat it as a username/userId
                actualUserId = userIdOrAccount;
            }
            
            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                customer = new CustomerLoyalty
                {
                    UserId = actualUserId,
                    TotalPoints = points,
                    Tier = CalculateTier(points),
                    LastUpdated = DateTime.UtcNow,
                    PointsExpiryDate = DateTime.UtcNow.AddYears(1)
                };
            }
            else
            {
                var oldPoints = customer.TotalPoints;
                customer.TotalPoints = points;
                customer.Tier = CalculateTier(points);
                customer.LastUpdated = DateTime.UtcNow;
                
                _logger.LogInformation("Points reset for {UserIdOrAccount}: {OldPoints} -> {NewPoints}", 
                    userIdOrAccount, oldPoints, points);
            }
            
            await _repository.UpdateCustomerAsync(customer);
            return true;
        }

        public async Task<LoyaltyRedemptionStatusDto> CheckRedemptionCreditStatusAsync(string userIdOrAccount, string transactionId)
        {
            _logger.LogInformation("Checking redemption credit status for {UserIdOrAccount}, Transaction: {TransactionId}", userIdOrAccount, transactionId);
            
            string actualUserId;
            if (IsValidAccountNumber(userIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {userIdOrAccount} not found.");
                }
            }
            else
            {
                actualUserId = userIdOrAccount;
            }

            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"User {userIdOrAccount} not found in loyalty program.");
            }

            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                // Check for credit transaction matching the redemption
                var sql = @"SELECT TOP 1 Amount, Txnstatus, transactiondate, Transactiontype, Memo
                           FROM KeystoneOmniTransactions 
                           WHERE (Craccount = @UserId OR Draccount = @UserId)
                           AND (Requestid = @TransactionId OR Memo LIKE '%' + @TransactionId + '%')
                           AND Transactiontype IN ('Credit', 'Transfer', 'NIP')
                           ORDER BY transactiondate DESC";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", actualUserId);
                command.Parameters.AddWithValue("@TransactionId", transactionId);
                
                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    var creditAmount = reader.GetDecimal(0);
                    var status = reader.GetString(1);
                    var transactionDate = reader.GetDateTime(2);
                    var transactionType = reader.GetString(3);
                    var memo = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    
                    // Extract loyalty points used from memo or calculate from amount
                    var loyaltyPointsUsed = ExtractLoyaltyPointsFromMemo(memo) ?? (int)creditAmount;
                    
                    return new LoyaltyRedemptionStatusDto
                    {
                        UserIdOrAccount = userIdOrAccount,
                        TransactionId = transactionId,
                        IsCredited = status == "00",
                        CreditAmount = creditAmount,
                        LoyaltyPointsUsed = loyaltyPointsUsed,
                        LoyaltyAmountValue = loyaltyPointsUsed * 1.0m,
                        PreviousPoints = customer.TotalPoints + loyaltyPointsUsed,
                        CurrentPoints = customer.TotalPoints,
                        TotalTransactionAmount = creditAmount,
                        TransactionDate = transactionDate,
                        Status = status == "00" ? "Success" : "Failed",
                        Message = status == "00" ? "Account successfully credited with loyalty redemption" : "Credit transaction failed"
                    };
                }
                else
                {
                    return new LoyaltyRedemptionStatusDto
                    {
                        UserIdOrAccount = userIdOrAccount,
                        TransactionId = transactionId,
                        IsCredited = false,
                        CurrentPoints = customer.TotalPoints,
                        Status = "Not Found",
                        Message = "No credit transaction found for this redemption"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking redemption credit status for {UserIdOrAccount}", userIdOrAccount);
                throw;
            }
        }

        private int? ExtractLoyaltyPointsFromMemo(string? memo)
        {
            if (string.IsNullOrEmpty(memo)) return null;
            
            // Look for patterns like "LoyaltyRedemption:500" or "Points:500"
            var patterns = new[] { "LoyaltyRedemption:", "Points:", "RED_" };
            
            foreach (var pattern in patterns)
            {
                var index = memo.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var valueStart = index + pattern.Length;
                    var valueEnd = memo.IndexOfAny(new[] { ' ', ',', ';', '|' }, valueStart);
                    if (valueEnd == -1) valueEnd = memo.Length;
                    
                    var valueStr = memo.Substring(valueStart, valueEnd - valueStart);
                    if (int.TryParse(valueStr, out var points))
                    {
                        return points;
                    }
                }
            }
            
            return null;
        }

        public async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(string userIdOrAccount)
        {
            _logger.LogInformation("Getting recent transactions for {UserIdOrAccount}", userIdOrAccount);
            
            string actualUserId;
            string username = userIdOrAccount;
            
            if (IsValidAccountNumber(userIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(userIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {userIdOrAccount} not found.");
                }
                // For account numbers, we need to find the username
                username = userIdOrAccount; // Keep original for username search
            }
            else
            {
                // If it's not an account number, treat it as a username
                actualUserId = userIdOrAccount;
                username = userIdOrAccount;
            }

            var transactions = new List<RecentTransactionDto>();
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                // Use separate connections for each query to avoid DataReader conflicts
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    // First check if any transactions exist for this user
                    var checkSql = "SELECT COUNT(*) FROM KeystoneOmniTransactions WHERE Draccount = @UserId";
                    using var checkCommand = new SqlCommand(checkSql, connection);
                    checkCommand.Parameters.AddWithValue("@UserId", actualUserId);
                    var count = (int)await checkCommand.ExecuteScalarAsync();
                    _logger.LogInformation("Found {Count} total transactions for user {UserId}", count, actualUserId);
                }
                
                // Check what usernames exist in the database (separate connection)
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var usersSql = "SELECT DISTINCT TOP 5 Draccount FROM KeystoneOmniTransactions WHERE Draccount IS NOT NULL AND Draccount != ''";
                    using var usersCommand = new SqlCommand(usersSql, connection);
                    using var usersReader = await usersCommand.ExecuteReaderAsync();
                    var existingUsers = new List<string>();
                    while (await usersReader.ReadAsync())
                    {
                        existingUsers.Add(usersReader.GetString(0));
                    }
                    _logger.LogInformation("Sample usernames in database: {Users}", string.Join(", ", existingUsers));
                }
                
                // Get the actual transactions (separate connection)
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"SELECT TOP 10 Requestid, Transactiontype, Amount, Txnstatus, transactiondate, Memo
                               FROM KeystoneOmniTransactions 
                               WHERE (Draccount = @UserId OR Username = @Username)
                               AND transactiondate > DATEADD(HOUR, -24, GETDATE())
                               ORDER BY transactiondate DESC";
                    
                    _logger.LogInformation("Executing SQL query for user {UserId}, username {Username}: {SQL}", actualUserId, username, sql);
                    
                    using var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@UserId", actualUserId);
                    command.Parameters.AddWithValue("@Username", username);
                    
                    using var reader = await command.ExecuteReaderAsync();
                    
                    while (await reader.ReadAsync())
                    {
                        var transactionRef = reader.GetString(0);
                        var transactionAmount = reader.GetDecimal(2);
                        var transactionDate = reader.GetDateTime(4);
                        var memo = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        
                        // Check if loyalty points were used by looking for point deductions around transaction time
                        var (usedLoyaltyPoints, pointsUsed) = await CheckLoyaltyPointUsageAsync(actualUserId, transactionRef, transactionAmount, transactionDate);
                        
                        transactions.Add(new RecentTransactionDto
                        {
                            TransactionReference = transactionRef,
                            TransactionType = reader.GetString(1),
                            Amount = transactionAmount,
                            Status = reader.GetString(3),
                            TransactionDate = transactionDate,
                            UsedLoyaltyPoints = usedLoyaltyPoints,
                            PointsUsed = pointsUsed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent transactions for {UserIdOrAccount}", userIdOrAccount);
                throw;
            }
            
            return transactions;
        }



        private async Task<(bool usedLoyaltyPoints, int pointsUsed)> CheckLoyaltyPointUsageAsync(string userId, string transactionRef, decimal transactionAmount, DateTime transactionDate)
        {
            try
            {
                // Check if customer's points were reduced around the transaction time
                var customer = await _repository.GetCustomerByUserIdAsync(userId);
                if (customer == null) return (false, 0);
                
                // Look for point deductions within 2 minutes of transaction
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                // Check customer loyalty history for point reductions around this time
                var sql = @"SELECT TOP 1 TotalPoints, LastUpdated 
                           FROM CustomerLoyalty 
                           WHERE UserId = @UserId 
                           AND ABS(DATEDIFF(MINUTE, LastUpdated, @TransactionDate)) <= 2
                           ORDER BY LastUpdated DESC";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@TransactionDate", transactionDate);
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Estimate points used based on transaction amount (1 point = ₦1)
                    var estimatedPointsUsed = (int)transactionAmount;
                    if (estimatedPointsUsed > 0 && estimatedPointsUsed <= 1000)
                    {
                        return (true, estimatedPointsUsed);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking loyalty usage for {TransactionRef}", transactionRef);
            }
            
            return (false, 0);
        }

        private static bool IsValidAccountNumber(string? accountNumber)
        {
            return !string.IsNullOrWhiteSpace(accountNumber) && 
                   accountNumber.Length == 10 && 
                   accountNumber.All(char.IsDigit);
        }
        
        public async Task<UsePointsResponse> UsePointsForTransactionAsync(UsePointsRequest request)
        {
            _logger.LogInformation("Processing use points request - User: {User}, Points: {Points}, Amount: {Amount}", 
                request.UserIdOrAccount, request.PointsToUse, request.TransactionAmount);
            
            string actualUserId;
            if (IsValidAccountNumber(request.UserIdOrAccount))
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(request.UserIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Account number {request.UserIdOrAccount} not found.");
                }
            }
            else
            {
                actualUserId = await _accountMapping.GetUserIdByAccountNumberAsync(request.UserIdOrAccount);
                if (string.IsNullOrEmpty(actualUserId))
                {
                    throw new KeyNotFoundException($"Username {request.UserIdOrAccount} not found.");
                }
            }

            var customer = await _repository.GetCustomerByUserIdAsync(actualUserId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"User {request.UserIdOrAccount} not found in loyalty program.");
            }

            if (customer.TotalPoints < request.PointsToUse)
            {
                return new UsePointsResponse
                {
                    Success = false,
                    Message = "Insufficient points",
                    AvailablePoints = customer.TotalPoints,
                    PointsUsed = 0,
                    RemainingPoints = customer.TotalPoints
                };
            }

            // Deduct points
            customer.TotalPoints -= request.PointsToUse;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            await _repository.UpdateCustomerAsync(customer);
            
            var pointValue = request.PointsToUse * 1.0m;
            
            return new UsePointsResponse
            {
                Success = true,
                Message = $"Successfully used {request.PointsToUse} points (₦{pointValue:N2})",
                AvailablePoints = customer.TotalPoints + request.PointsToUse,
                PointsUsed = request.PointsToUse,
                RemainingPoints = customer.TotalPoints,
                TransactionReference = request.TransactionReference
            };
        }
        
        private int CalculatePointsForTransaction(string transactionType, decimal amount)
        {
            if (amount < 100m) return 0;
            
            return transactionType.ToUpper() switch
            {
                var type when type.Contains("AIRTIME") || type.Contains("DATA") || type == "AIRTIME" || type == "MOBILEDATA" => 1,
                var type when type.Contains("BILL") || type.Contains("PAYMENT") || type == "BILLSPAYMENT" => 3,
                var type when type.Contains("TRANSFER") || type.Contains("NIP") || type == "INTERNAL" || type == "INTERBANK" => 2,
                _ => 0
            };
        }
    }
}