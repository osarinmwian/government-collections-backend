using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Services;

public interface IProcessedTransactionService
{
    Task<bool> IsTransactionProcessedAsync(string transactionId);
    Task MarkTransactionAsProcessedAsync(string transactionId);
}

public class ProcessedTransactionService : IProcessedTransactionService
{
    private readonly string _connectionString;
    private readonly ILogger<ProcessedTransactionService> _logger;
    private bool _tableCreated = false;

    public ProcessedTransactionService(IConfiguration configuration, ILogger<ProcessedTransactionService> logger)
    {
        _connectionString = configuration.GetConnectionString("OmniDbConnection")!;
        _logger = logger;
    }

    private async Task EnsureTableExistsAsync()
    {
        if (_tableCreated) return;
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProcessedTransactions' AND xtype='U')
                BEGIN
                    CREATE TABLE ProcessedTransactions (
                        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                        TransactionId NVARCHAR(100) NOT NULL UNIQUE,
                        ProcessedDate DATETIME2 NOT NULL DEFAULT GETDATE()
                    );
                    CREATE INDEX IX_ProcessedTransactions_TransactionId ON ProcessedTransactions(TransactionId);
                    CREATE INDEX IX_ProcessedTransactions_ProcessedDate ON ProcessedTransactions(ProcessedDate);
                END";
            
            using var command = new SqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();
            _tableCreated = true;
            _logger.LogInformation("ProcessedTransactions table ensured to exist");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create ProcessedTransactions table");
            throw;
        }
    }

    public async Task<bool> IsTransactionProcessedAsync(string transactionId)
    {
        await EnsureTableExistsAsync();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT COUNT(1) FROM ProcessedTransactions WHERE TransactionId = @TransactionId";
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            var isProcessed = count > 0;
            
            _logger.LogDebug("Transaction {TransactionId} processed check: {IsProcessed}", transactionId, isProcessed);
            return isProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if transaction {TransactionId} is processed", transactionId);
            return true; // Return true on error to prevent duplicate processing
        }
    }

    public async Task MarkTransactionAsProcessedAsync(string transactionId)
    {
        await EnsureTableExistsAsync();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                IF NOT EXISTS (SELECT 1 FROM ProcessedTransactions WHERE TransactionId = @TransactionId)
                INSERT INTO ProcessedTransactions (TransactionId, ProcessedDate) 
                VALUES (@TransactionId, GETDATE())";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@TransactionId", transactionId);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Transaction {TransactionId} marked as processed (rows affected: {Rows})", transactionId, rowsAffected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking transaction {TransactionId} as processed", transactionId);
            throw;
        }
    }
}