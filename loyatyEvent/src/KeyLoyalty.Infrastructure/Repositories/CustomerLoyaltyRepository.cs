using KeyLoyalty.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Repositories
{
    public class CustomerLoyaltyRepository : ICustomerLoyaltyRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<CustomerLoyaltyRepository> _logger;

        public CustomerLoyaltyRepository(IConfiguration configuration, ILogger<CustomerLoyaltyRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("OmniDbConnection")!;
            _logger = logger;
        }

        public async Task<CustomerLoyalty?> GetCustomerByUserIdAsync(string userId)
        {
            _logger.LogInformation("Getting customer with UserId: {UserId}", userId);
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT UserId, TotalPoints, Tier, LastUpdated, PointsExpiryDate FROM CustomerLoyalty WHERE UserId = @UserId";
                
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", userId);
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var customer = new CustomerLoyalty
                    {
                        UserId = reader.GetString(0),
                        TotalPoints = reader.GetInt32(1),
                        Tier = (LoyaltyTier)reader.GetInt32(2),
                        LastUpdated = reader.GetDateTime(3),
                        PointsExpiryDate = reader.IsDBNull(4) ? DateTime.UtcNow.AddYears(1) : reader.GetDateTime(4)
                    };
                    _logger.LogInformation("Customer found: {UserId}, Points: {Points}, Tier: {Tier}", 
                        customer.UserId, customer.TotalPoints, customer.Tier);
                    return customer;
                }
                
                _logger.LogWarning("Customer not found with UserId: {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        public async Task CreateCustomerAsync(CustomerLoyalty customer)
        {
            await UpdateCustomerAsync(customer);
        }

        public async Task UpdateCustomerAsync(CustomerLoyalty customer)
        {
            _logger.LogInformation("Updating customer: {UserId}, Points: {Points}, Tier: {Tier}", 
                customer.UserId, customer.TotalPoints, customer.Tier);
            
            try
            {
                using var connection = new SqlConnection(_connectionString);
                _logger.LogDebug("Opening database connection for update");
                await connection.OpenAsync();
                
                var sql = @"
                    MERGE CustomerLoyalty AS target
                    USING (SELECT @UserId AS UserId, @TotalPoints AS TotalPoints, @Tier AS Tier, @LastUpdated AS LastUpdated, @PointsExpiryDate AS PointsExpiryDate) AS source
                    ON target.UserId = source.UserId
                    WHEN MATCHED THEN
                        UPDATE SET TotalPoints = source.TotalPoints, Tier = source.Tier, LastUpdated = source.LastUpdated, PointsExpiryDate = source.PointsExpiryDate
                    WHEN NOT MATCHED THEN
                        INSERT (UserId, TotalPoints, Tier, LastUpdated, PointsExpiryDate)
                        VALUES (source.UserId, source.TotalPoints, source.Tier, source.LastUpdated, source.PointsExpiryDate);";
                
                _logger.LogDebug("Executing MERGE SQL for UserId: {UserId}", customer.UserId);
                
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@UserId", customer.UserId);
                command.Parameters.AddWithValue("@TotalPoints", customer.TotalPoints);
                command.Parameters.AddWithValue("@Tier", (int)customer.Tier);
                command.Parameters.AddWithValue("@LastUpdated", customer.LastUpdated);
                command.Parameters.AddWithValue("@PointsExpiryDate", customer.PointsExpiryDate);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Customer update completed. Rows affected: {RowsAffected} for UserId: {UserId}", 
                    rowsAffected, customer.UserId);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error updating customer {UserId}: {Message}", customer.UserId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating customer {UserId}: {Message}", customer.UserId, ex.Message);
                throw;
            }
        }

        // Stub implementations for interface compliance
        public Task<List<PointTransaction>> GetPointTransactionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 20, int pageNumber = 1) => Task.FromResult(new List<PointTransaction>());
        public Task CreatePointTransactionAsync(PointTransaction transaction) => Task.CompletedTask;
        public Task<List<PointTransaction>> GetExpiringPointsAsync(string userId, DateTime expiryDate) => Task.FromResult(new List<PointTransaction>());
        public Task ExpirePointsAsync(List<string> transactionIds) => Task.CompletedTask;
        public Task<List<PointRedemption>> GetPointRedemptionsAsync(string userId, DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 20, int pageNumber = 1) => Task.FromResult(new List<PointRedemption>());
        public Task CreatePointRedemptionAsync(PointRedemption redemption) => Task.CompletedTask;
        public Task<List<LoyaltyAlert>> GetAlertsAsync(string userId, bool unreadOnly = false) => Task.FromResult(new List<LoyaltyAlert>());
        public Task CreateAlertAsync(LoyaltyAlert alert) => Task.CompletedTask;
        public Task MarkAlertAsReadAsync(string alertId) => Task.CompletedTask;
        public Task MarkAllAlertsAsReadAsync(string userId) => Task.CompletedTask;
        public Task<List<PointTransaction>> GetRecentTransactionsAsync(string userId, TimeSpan timeSpan) => Task.FromResult(new List<PointTransaction>());
        public Task<List<PointRedemption>> GetRecentRedemptionsAsync(string userId, TimeSpan timeSpan) => Task.FromResult(new List<PointRedemption>());
        
        public async Task<List<CustomerLoyalty>> GetCustomersWithExpiredPointsAsync(DateTime currentDate)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT UserId, TotalPoints, Tier, LastUpdated, PointsExpiryDate FROM CustomerLoyalty WHERE PointsExpiryDate < @CurrentDate AND TotalPoints > 0";
                
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@CurrentDate", currentDate);
                
                var customers = new List<CustomerLoyalty>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerLoyalty
                    {
                        UserId = reader.GetString(0),
                        TotalPoints = reader.GetInt32(1),
                        Tier = (LoyaltyTier)reader.GetInt32(2),
                        LastUpdated = reader.GetDateTime(3),
                        PointsExpiryDate = reader.GetDateTime(4)
                    });
                }
                
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers with expired points");
                return new List<CustomerLoyalty>();
            }
        }
        
        public async Task<List<CustomerLoyalty>> GetCustomersWithPointsExpiringOnAsync(DateTime targetDate)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT UserId, TotalPoints, Tier, LastUpdated, PointsExpiryDate FROM CustomerLoyalty WHERE CAST(PointsExpiryDate AS DATE) = @TargetDate AND TotalPoints > 0";
                
                var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@TargetDate", targetDate.Date);
                
                var customers = new List<CustomerLoyalty>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerLoyalty
                    {
                        UserId = reader.GetString(0),
                        TotalPoints = reader.GetInt32(1),
                        Tier = (LoyaltyTier)reader.GetInt32(2),
                        LastUpdated = reader.GetDateTime(3),
                        PointsExpiryDate = reader.GetDateTime(4)
                    });
                }
                
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers with points expiring on {TargetDate}", targetDate);
                return new List<CustomerLoyalty>();
            }
        }

        public async Task<List<CustomerLoyalty>> GetAllCustomersAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sql = "SELECT UserId, TotalPoints, Tier, LastUpdated, PointsExpiryDate FROM CustomerLoyalty";
                
                var command = new SqlCommand(sql, connection);
                var customers = new List<CustomerLoyalty>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    customers.Add(new CustomerLoyalty
                    {
                        UserId = reader.GetString(0),
                        TotalPoints = reader.GetInt32(1),
                        Tier = (LoyaltyTier)reader.GetInt32(2),
                        LastUpdated = reader.GetDateTime(3),
                        PointsExpiryDate = reader.IsDBNull(4) ? DateTime.UtcNow.AddYears(1) : reader.GetDateTime(4)
                    });
                }
                
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return new List<CustomerLoyalty>();
            }
        }
    }
}