using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KeyLoyalty.Infrastructure.Services
{
    public interface ITransactionLoggerService
    {
        Task LogAllTransactions();
        Task LogInboundTransactions();
        Task LogOutboundTransactions();
    }

    public class TransactionLoggerService : ITransactionLoggerService
    {
        private readonly ILogger<TransactionLoggerService> _logger;
        private readonly IConfiguration _configuration;

        public TransactionLoggerService(
            IConfiguration configuration,
            ILogger<TransactionLoggerService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task LogAllTransactions()
        {
            await LogInboundTransactions();
            await LogOutboundTransactions();
        }

        public async Task LogInboundTransactions()
        {
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT TOP 50 
                           Requestid, Draccount, Craccount, Amount, Transactiontype, 
                           Txnstatus, transactiondate, Username, Usernetwork, 
                           Billername, Billerproduct, Narration, Sessionid
                           FROM KeystoneOmniTransactions 
                           WHERE transactiondate > DATEADD(MINUTE, -2, GETDATE())
                           AND (Draccount IS NOT NULL OR Craccount IS NOT NULL)
                           ORDER BY transactiondate DESC";
                
                var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var transactions = new List<object>();
                
                while (await reader.ReadAsync())
                {
                    var transaction = new
                    {
                        TransactionId = reader.IsDBNull(0) ? "" : reader.GetString(0),
                        DebitAccount = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        CreditAccount = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Amount = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        TransactionType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Status = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        TransactionDate = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                        Username = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Network = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        BillerName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        BillerProduct = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        Narration = reader.IsDBNull(11) ? "" : reader.GetString(11),
                        SessionId = reader.IsDBNull(12) ? "" : reader.GetString(12),
                        Direction = "INBOUND",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    transactions.Add(transaction);
                }
                
                _logger.LogInformation("INBOUND_TRANSACTIONS: {Count} transactions retrieved from database", transactions.Count);
                
                foreach (var txn in transactions)
                {
                    _logger.LogInformation("INBOUND_TXN: {Transaction}", JsonSerializer.Serialize(txn));
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error retrieving inbound transactions: {Message}", ex.Message);
            }
        }

        public async Task LogOutboundTransactions()
        {
            var connectionString = _configuration.GetConnectionString("OmniDbConnection");
            
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                
                var sql = @"SELECT TOP 50 
                           Requestid, Draccount, Craccount, Amount, Transactiontype, 
                           Txnstatus, transactiondate, Username, Usernetwork, 
                           Billername, Billerproduct, Narration, Sessionid
                           FROM KeystoneOmniTransactions 
                           WHERE transactiondate > DATEADD(MINUTE, -2, GETDATE())
                           AND Txnstatus = '00'
                           AND Transactiontype IN ('Airtime', 'MobileData', 'BillsPayment', 'NIP', 'Internal', 'OwnInternal', 'InterBank', 'NQR')
                           ORDER BY transactiondate DESC";
                
                var command = new SqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                var transactions = new List<object>();
                
                while (await reader.ReadAsync())
                {
                    var transaction = new
                    {
                        TransactionId = reader.IsDBNull(0) ? "" : reader.GetString(0),
                        DebitAccount = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        CreditAccount = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Amount = reader.IsDBNull(3) ? 0 : reader.GetDecimal(3),
                        TransactionType = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Status = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        TransactionDate = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6),
                        Username = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Network = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        BillerName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        BillerProduct = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        Narration = reader.IsDBNull(11) ? "" : reader.GetString(11),
                        SessionId = reader.IsDBNull(12) ? "" : reader.GetString(12),
                        Direction = "OUTBOUND",
                        LoyaltyEligible = true,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    transactions.Add(transaction);
                }
                
                _logger.LogInformation("OUTBOUND_TRANSACTIONS: {Count} loyalty-eligible transactions retrieved", transactions.Count);
                
                foreach (var txn in transactions)
                {
                    _logger.LogInformation("OUTBOUND_TXN: {Transaction}", JsonSerializer.Serialize(txn));
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error retrieving outbound transactions: {Message}", ex.Message);
            }
        }
    }
}