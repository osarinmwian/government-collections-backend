using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using KeyLoyalty.Domain.Entities;

namespace KeyLoyalty.Infrastructure.Services;

public interface IAccountingService
{
    Task<string> ProcessRedemptionAccountingAsync(string accountNumber, decimal pointsRedeemed, decimal cashValue, string redemptionType);
}

public class AccountingService : IAccountingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountingService> _logger;

    public AccountingService(IConfiguration configuration, ILogger<AccountingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> ProcessRedemptionAccountingAsync(string accountNumber, decimal pointsRedeemed, decimal cashValue, string redemptionType)
    {
        var transactionId = Guid.NewGuid().ToString();
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ProcessAccountingWithRetryAsync(transactionId, accountNumber, pointsRedeemed, cashValue, redemptionType);
            }
            catch (Exception ex) when (attempt < maxRetries && IsTransientError(ex))
            {
                _logger.LogWarning(ex, "Accounting attempt {Attempt}/{MaxRetries} failed for transaction {TransactionId}. Retrying in {Delay}ms", 
                    attempt, maxRetries, transactionId, delay.TotalMilliseconds);
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }
        
        throw new InvalidOperationException($"Failed to process accounting after {maxRetries} attempts for transaction {transactionId}");
    }
    
    private async Task<string> ProcessAccountingWithRetryAsync(string transactionId, string accountNumber, decimal pointsRedeemed, decimal cashValue, string redemptionType)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        using var transaction = connection.BeginTransaction();
        
        try
        {
            // Get GL accounts from configuration
            var loyaltyLiabilityGL = _configuration["KeyLoyalty:Accounting:LoyaltyPointsLiabilityGL"] ?? "";
            var redemptionExpenseGL = _configuration["KeyLoyalty:Accounting:LoyaltyRedemptionExpenseGL"] ?? "";
            var cashGL = _configuration["KeyLoyalty:Accounting:CashAccountGL"] ?? "";
            var airtimeGL = _configuration["KeyLoyalty:Accounting:AirtimePurchaseGL"] ?? "";

            // Create accounting entries
            var entries = new List<AccountingEntry>();

            switch (redemptionType.ToUpper())
            {
                case "CASH":
                    entries.Add(new AccountingEntry
                    {
                        GLAccount = redemptionExpenseGL,
                        DebitAmount = cashValue,
                        CreditAmount = 0,
                        Description = $"Loyalty points redemption - Cash for {accountNumber}"
                    });
                    
                    entries.Add(new AccountingEntry
                    {
                        GLAccount = cashGL,
                        DebitAmount = 0,
                        CreditAmount = cashValue,
                        Description = $"Cash payout for loyalty redemption - {accountNumber}"
                    });
                    break;

                case "AIRTIME":
                    entries.Add(new AccountingEntry
                    {
                        GLAccount = redemptionExpenseGL,
                        DebitAmount = cashValue,
                        CreditAmount = 0,
                        Description = $"Loyalty points redemption - Airtime for {accountNumber}"
                    });
                    
                    entries.Add(new AccountingEntry
                    {
                        GLAccount = airtimeGL,
                        DebitAmount = 0,
                        CreditAmount = cashValue,
                        Description = $"Airtime purchase for loyalty redemption - {accountNumber}"
                    });
                    break;
            }

            // Debit: Loyalty Points Liability (reduce liability)
            entries.Add(new AccountingEntry
            {
                GLAccount = loyaltyLiabilityGL,
                DebitAmount = cashValue,
                CreditAmount = 0,
                Description = $"Reduce loyalty points liability - {pointsRedeemed} points redeemed by {accountNumber}"
            });

            // Insert all accounting entries in transaction
            foreach (var entry in entries)
            {
                var sql = @"INSERT INTO AccountingEntries (TransactionId, GLAccount, DebitAmount, CreditAmount, Description, CreatedDate, AccountNumber) 
                           VALUES (@TransactionId, @GLAccount, @DebitAmount, @CreditAmount, @Description, @CreatedDate, @AccountNumber)";
                
                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@TransactionId", transactionId);
                command.Parameters.AddWithValue("@GLAccount", entry.GLAccount);
                command.Parameters.AddWithValue("@DebitAmount", entry.DebitAmount);
                command.Parameters.AddWithValue("@CreditAmount", entry.CreditAmount);
                command.Parameters.AddWithValue("@Description", entry.Description);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                command.Parameters.AddWithValue("@AccountNumber", accountNumber);
                
                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation("Accounting Entry - GL: {GL}, Debit: {Debit}, Credit: {Credit}, Desc: {Description}", 
                    entry.GLAccount, entry.DebitAmount, entry.CreditAmount, entry.Description);
            }

            // Commit transaction
            await transaction.CommitAsync();
            _logger.LogInformation("Accounting transaction {TransactionId} committed successfully", transactionId);
            
            return transactionId;
        }
        catch (Exception ex)
        {
            // Rollback transaction on error
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Accounting transaction {TransactionId} failed and rolled back", transactionId);
            throw;
        }
    }
    
    private static bool IsTransientError(Exception ex)
    {
        return ex is SqlException sqlEx && (sqlEx.Number == 2 || sqlEx.Number == 53 || sqlEx.Number == -2 || sqlEx.Number == 1205);
    }
}