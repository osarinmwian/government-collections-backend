using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KeyLoyalty.Infrastructure.Repositories;

namespace KeyLoyalty.Infrastructure.Services;

public interface ILoyaltyTransactionTracker
{
    Task<bool> DeductPointsForTransactionAsync(string accountNumber, decimal amount, string transactionId, string type);
    Task ConfirmTransactionAsync(string transactionId);
    Task RollbackTransactionAsync(string transactionId, string reason);
    Task SendTransactionEmailAsync(string accountNumber, string transactionType, decimal amount, int pointsUsed, bool isSuccess);
    Task SendTransactionSmsAsync(string accountNumber, string transactionType, decimal amount, int pointsUsed, bool isSuccess);
}

public class LoyaltyTransactionTracker : ILoyaltyTransactionTracker
{
    private readonly string _connectionString;
    private readonly ILogger<LoyaltyTransactionTracker> _logger;
    private readonly ICustomerLoyaltyRepository _loyaltyRepository;
    private readonly IAccountMappingService _accountMapping;
    private readonly HttpClient _httpClient;

    public LoyaltyTransactionTracker(IConfiguration configuration, ILogger<LoyaltyTransactionTracker> logger, 
        ICustomerLoyaltyRepository loyaltyRepository, IAccountMappingService accountMapping, HttpClient httpClient)
    {
        _connectionString = configuration.GetConnectionString("OmniDbConnection")!;
        _logger = logger;
        _loyaltyRepository = loyaltyRepository;
        _accountMapping = accountMapping;
        _httpClient = httpClient;
    }

    public async Task<bool> DeductPointsForTransactionAsync(string accountNumber, decimal amount, string transactionId, string type)
    {
        try
        {
            var userId = await _accountMapping.GetUserIdByAccountNumberAsync(accountNumber);
            if (string.IsNullOrEmpty(userId)) return false;

            var customer = await _loyaltyRepository.GetCustomerByUserIdAsync(userId);
            if (customer == null) return false;

            var pointsNeeded = (int)Math.Ceiling(amount);
            if (customer.TotalPoints < pointsNeeded) return false;

            // Store original points for rollback
            await StoreTransactionAsync(transactionId, accountNumber, pointsNeeded, amount, type, customer.TotalPoints);

            // Deduct points
            customer.TotalPoints -= pointsNeeded;
            customer.LastUpdated = DateTime.UtcNow;
            await _loyaltyRepository.UpdateCustomerAsync(customer);

            _logger.LogInformation("Deducted {Points} points for transaction {TransactionId}", pointsNeeded, transactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting points for transaction {TransactionId}", transactionId);
            return false;
        }
    }

    public async Task ConfirmTransactionAsync(string transactionId)
    {
        try
        {
            var transactionData = await GetTransactionDataAsync(transactionId);
            
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "UPDATE LoyaltyTransactionTracker SET Status = 'CONFIRMED' WHERE TransactionId = @TransactionId";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            await command.ExecuteNonQueryAsync();
            
            if (transactionData != null)
            {
                await SendTransactionEmailAsync(transactionData.AccountNumber, transactionData.TransactionType, 
                    transactionData.AmountUsed, transactionData.PointsUsed, true);
                await SendTransactionSmsAsync(transactionData.AccountNumber, transactionData.TransactionType, 
                    transactionData.AmountUsed, transactionData.PointsUsed, true);
            }
            
            _logger.LogInformation("Confirmed loyalty transaction: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming transaction: {TransactionId}", transactionId);
        }
    }

    public async Task RollbackTransactionAsync(string transactionId, string reason)
    {
        try
        {
            // Get transaction details
            var transactionData = await GetTransactionDataAsync(transactionId);
            if (transactionData == null) return;

            // Restore points
            var userId = await _accountMapping.GetUserIdByAccountNumberAsync(transactionData.AccountNumber);
            if (!string.IsNullOrEmpty(userId))
            {
                var customer = await _loyaltyRepository.GetCustomerByUserIdAsync(userId);
                if (customer != null)
                {
                    customer.TotalPoints = transactionData.OriginalPoints;
                    customer.LastUpdated = DateTime.UtcNow;
                    await _loyaltyRepository.UpdateCustomerAsync(customer);
                }
            }

            // Update tracker
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var sql = "UPDATE LoyaltyTransactionTracker SET Status = 'ROLLED_BACK', RollbackReason = @Reason WHERE TransactionId = @TransactionId";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            command.Parameters.AddWithValue("@Reason", reason);
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Rolled back loyalty transaction: {TransactionId}, Points restored", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction: {TransactionId}", transactionId);
        }
    }

    private async Task StoreTransactionAsync(string transactionId, string accountNumber, int points, decimal amount, string type, int originalPoints)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = @"
            INSERT INTO LoyaltyTransactionTracker 
            (TransactionId, AccountNumber, PointsUsed, AmountUsed, TransactionType, OriginalPoints, Status, CreatedDate)
            VALUES (@TransactionId, @AccountNumber, @Points, @Amount, @Type, @OriginalPoints, 'PENDING', GETDATE())";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionId", transactionId);
        command.Parameters.AddWithValue("@AccountNumber", accountNumber);
        command.Parameters.AddWithValue("@Points", points);
        command.Parameters.AddWithValue("@Amount", amount);
        command.Parameters.AddWithValue("@Type", type);
        command.Parameters.AddWithValue("@OriginalPoints", originalPoints);
        
        await command.ExecuteNonQueryAsync();
    }

    public async Task SendTransactionEmailAsync(string accountNumber, string transactionType, decimal amount, int pointsUsed, bool isSuccess)
    {
        try
        {
            var emailRequest = new
            {
                AccountNumber = accountNumber,
                TransactionType = transactionType,
                Amount = amount,
                PointsUsed = pointsUsed,
                IsSuccess = isSuccess,
                Subject = isSuccess ? "Loyalty Points Transaction Successful" : "Loyalty Points Transaction Failed",
                Message = isSuccess 
                    ? $"You successfully used {pointsUsed} loyalty points (₦{amount:N2}) for {transactionType.ToLower()}."
                    : $"Your {transactionType.ToLower()} transaction failed. {pointsUsed} loyalty points have been restored to your account."
            };

            var json = System.Text.Json.JsonSerializer.Serialize(emailRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            await _httpClient.PostAsync("/api/notifications/loyalty-transaction-email", content);
            _logger.LogInformation("Email notification sent for transaction - Account: {Account}, Type: {Type}", accountNumber, transactionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction email for account {Account}", accountNumber);
        }
    }

    public async Task SendTransactionSmsAsync(string accountNumber, string transactionType, decimal amount, int pointsUsed, bool isSuccess)
    {
        try
        {
            var smsRequest = new
            {
                AccountNumber = accountNumber,
                TransactionType = transactionType,
                Amount = amount,
                PointsUsed = pointsUsed,
                IsSuccess = isSuccess,
                Message = isSuccess 
                    ? $"Loyalty: You used {pointsUsed} points (₦{amount:N2}) for {transactionType.ToLower()}. Transaction successful."
                    : $"Loyalty: {transactionType} failed. {pointsUsed} points restored to your account."
            };

            var json = System.Text.Json.JsonSerializer.Serialize(smsRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            await _httpClient.PostAsync("/api/notifications/loyalty-transaction-sms", content);
            _logger.LogInformation("SMS notification sent for transaction - Account: {Account}, Type: {Type}", accountNumber, transactionType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending transaction SMS for account {Account}", accountNumber);
        }
    }

    private async Task<TransactionData?> GetTransactionDataAsync(string transactionId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var sql = "SELECT AccountNumber, OriginalPoints, TransactionType, AmountUsed, PointsUsed FROM LoyaltyTransactionTracker WHERE TransactionId = @TransactionId";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionId", transactionId);
        
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new TransactionData
            {
                AccountNumber = reader.GetString(0),
                OriginalPoints = reader.GetInt32(1),
                TransactionType = reader.GetString(2),
                AmountUsed = reader.GetDecimal(3),
                PointsUsed = reader.GetInt32(4)
            };
        }
        return null;
    }

    private class TransactionData
    {
        public string AccountNumber { get; set; } = string.Empty;
        public int OriginalPoints { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal AmountUsed { get; set; }
        public int PointsUsed { get; set; }
    }
}