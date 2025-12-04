using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace KeyLoyalty.Infrastructure.Services;

public interface IAccountMappingService
{
    Task<string?> GetUserIdByAccountNumberAsync(string accountNumber);
    Task<List<string>> GetAccountNumbersByUserIdAsync(string userId);
}

public class AccountMappingService : IAccountMappingService
{
    private readonly string _connectionString;
    private readonly ILogger<AccountMappingService> _logger;

    public AccountMappingService(IConfiguration configuration, ILogger<AccountMappingService> logger)
    {
        _connectionString = configuration.GetConnectionString("OmniDbConnection")!;
        _logger = logger;
    }

    public async Task<string?> GetUserIdByAccountNumberAsync(string accountNumber)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            // If input looks like a username (not 10 digits), try to find the account number
            if (!IsValidAccountNumber(accountNumber))
            {
                _logger.LogInformation("Input {Input} appears to be a username, searching for account number", accountNumber);
                
                // Try to find account number by username in KeystoneOmniTransactions
                var usernameSql = "SELECT TOP 1 Draccount FROM KeystoneOmniTransactions WHERE Username = @Username OR Draccount LIKE '%' + @Username + '%'";
                using var usernameCommand = new SqlCommand(usernameSql, connection);
                usernameCommand.Parameters.AddWithValue("@Username", accountNumber);
                
                var accountResult = await usernameCommand.ExecuteScalarAsync();
                if (accountResult != null)
                {
                    var foundAccount = accountResult.ToString()!;
                    _logger.LogInformation("Username {Username} mapped to account {Account}", accountNumber, foundAccount);
                    return foundAccount; // Return the account number as userId
                }
                
                _logger.LogWarning("Username {Username} not found in transactions", accountNumber);
                return null;
            }
            
            // Use Draccount as both account number and user identifier since Username column may not exist
            var sql = "SELECT TOP 1 Draccount FROM KeystoneOmniTransactions WHERE Draccount = @AccountNumber";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@AccountNumber", accountNumber);
            
            var result = await command.ExecuteScalarAsync();
            var userId = result?.ToString() ?? accountNumber; // Use account number as userId if not found
            
            _logger.LogInformation("Account {AccountNumber} mapped to UserId {UserId}", accountNumber, userId);
            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping account {AccountNumber} to UserId", accountNumber);
            // Return null for usernames, account number for valid account numbers
            return IsValidAccountNumber(accountNumber) ? accountNumber : null;
        }
    }
    
    private static bool IsValidAccountNumber(string? accountNumber)
    {
        return !string.IsNullOrWhiteSpace(accountNumber) && 
               accountNumber.Length == 10 && 
               accountNumber.All(char.IsDigit);
    }

    public async Task<List<string>> GetAccountNumbersByUserIdAsync(string userId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var accountNumbers = new List<string>();
            
            // First, try to find all accounts associated with this user
            // Look for accounts with same username or phone number patterns
            var sql = @"
                SELECT DISTINCT Draccount 
                FROM KeystoneOmniTransactions 
                WHERE (Draccount = @UserId 
                   OR Username IN (SELECT DISTINCT Username FROM KeystoneOmniTransactions WHERE Draccount = @UserId AND Username IS NOT NULL)
                   OR Draccount IN (SELECT DISTINCT Draccount FROM KeystoneOmniTransactions WHERE Username IN 
                       (SELECT DISTINCT Username FROM KeystoneOmniTransactions WHERE Draccount = @UserId AND Username IS NOT NULL)))
                AND Draccount IS NOT NULL 
                AND LEN(Draccount) = 10
                AND ISNUMERIC(Draccount) = 1";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var account = reader.GetString(0);
                if (!accountNumbers.Contains(account))
                {
                    accountNumbers.Add(account);
                }
            }
            
            // If no accounts found, add the userId itself if it's a valid account number
            if (accountNumbers.Count == 0 && IsValidAccountNumber(userId))
            {
                accountNumbers.Add(userId);
            }
            
            _logger.LogInformation("UserId {UserId} mapped to {Count} account numbers: {Accounts}", 
                userId, accountNumbers.Count, string.Join(", ", accountNumbers));
            return accountNumbers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting account numbers for UserId {UserId}", userId);
            // Return userId as account number as fallback if it's valid
            return IsValidAccountNumber(userId) ? new List<string> { userId } : new List<string>();
        }
    }
}