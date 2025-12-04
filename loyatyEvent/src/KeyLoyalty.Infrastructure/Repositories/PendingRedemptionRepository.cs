using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Repositories
{
    public interface IPendingRedemptionRepository
    {
        Task<string> CreatePendingRedemptionAsync(string userId, int points, decimal amount, string transactionType, string transactionRef);
        Task<bool> ConfirmRedemptionAsync(string redemptionId, bool success);
        Task<(string userId, int points)> GetPendingRedemptionAsync(string redemptionId);
    }

    public class PendingRedemptionRepository : IPendingRedemptionRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<PendingRedemptionRepository> _logger;

        public PendingRedemptionRepository(IConfiguration configuration, ILogger<PendingRedemptionRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("OmniDbConnection")!;
            _logger = logger;
        }

        public async Task<string> CreatePendingRedemptionAsync(string userId, int points, decimal amount, string transactionType, string transactionRef)
        {
            var redemptionId = $"PND_{DateTime.Now.Ticks}";
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = @"INSERT INTO PendingRedemptions (RedemptionId, UserId, Points, Amount, TransactionType, TransactionRef, CreatedDate, Status)
                           VALUES (@RedemptionId, @UserId, @Points, @Amount, @TransactionType, @TransactionRef, GETDATE(), 'PENDING')";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@RedemptionId", redemptionId);
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@Points", points);
                command.Parameters.AddWithValue("@Amount", amount);
                command.Parameters.AddWithValue("@TransactionType", transactionType);
                command.Parameters.AddWithValue("@TransactionRef", transactionRef);
                
                await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Created pending redemption {RedemptionId} for user {UserId}", redemptionId, userId);
                
                return redemptionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending redemption for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ConfirmRedemptionAsync(string redemptionId, bool success)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var status = success ? "CONFIRMED" : "FAILED";
                var sql = @"UPDATE PendingRedemptions 
                           SET Status = @Status, ConfirmedDate = GETDATE() 
                           WHERE RedemptionId = @RedemptionId AND Status = 'PENDING'";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@RedemptionId", redemptionId);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming redemption {RedemptionId}", redemptionId);
                return false;
            }
        }

        public async Task<(string userId, int points)> GetPendingRedemptionAsync(string redemptionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT UserId, Points FROM PendingRedemptions WHERE RedemptionId = @RedemptionId AND Status = 'PENDING'";
                
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@RedemptionId", redemptionId);
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return (reader.GetString(0), reader.GetInt32(1));
                }
                
                return (string.Empty, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending redemption {RedemptionId}", redemptionId);
                return (string.Empty, 0);
            }
        }
    }
}