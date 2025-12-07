using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GovernmentCollections.Shared.Validation;

public class PinValidationService : IPinValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PinValidationService> _logger;
    private const int CommandTimeoutSeconds = 30;

    public PinValidationService(IConfiguration configuration, ILogger<PinValidationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidatePinAsync(string customerId, string pin)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            _logger.LogWarning("PIN validation failed: CustomerId is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(pin))
        {
            _logger.LogWarning("PIN validation failed: PIN is null or empty for customer {CustomerId}", customerId);
            return false;
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("Database connection string not configured");
                return false;
            }
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            const string query = "SELECT transactionpin, bvn, pinstatus FROM OmniProfiles WHERE customerid = @CustomerId AND profilestatus = 'Active'";
            using var command = new SqlCommand(query, connection) { CommandTimeout = CommandTimeoutSeconds };
            command.Parameters.Add("@CustomerId", SqlDbType.NVarChar, 50).Value = customerId;
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedPin = reader["transactionpin"]?.ToString();
                var bvn = reader["bvn"]?.ToString();
                var pinStatus = reader["pinstatus"]?.ToString();
                
                if (string.IsNullOrEmpty(storedPin) || string.IsNullOrEmpty(bvn))
                {
                    _logger.LogWarning("PIN validation failed: Missing PIN or BVN for customer {CustomerId}", customerId);
                    return false;
                }

                if (!string.Equals(pinStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("PIN validation failed: PIN status is {PinStatus} for customer {CustomerId}", pinStatus, customerId);
                    return false;
                }
                    
                var hashedPin = HashCustomerPin(pin, bvn);
                var isValid = string.Equals(hashedPin, storedPin, StringComparison.Ordinal);
                
                _logger.LogInformation("PIN validation {Result} for customer {CustomerId}", isValid ? "successful" : "failed", customerId);
                return isValid;
            }
            
            _logger.LogWarning("PIN validation failed: Customer {CustomerId} not found or inactive", customerId);
            return false;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during PIN validation for customer {CustomerId}", customerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating PIN for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> Validate2FAAsync(string customerId, string secondFa, string secondFaType)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            _logger.LogWarning("2FA validation failed: CustomerId is null or empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(secondFa))
        {
            _logger.LogWarning("2FA validation failed: SecondFa is null or empty for customer {CustomerId}", customerId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(secondFaType))
        {
            _logger.LogWarning("2FA validation failed: SecondFaType is null or empty for customer {CustomerId}", customerId);
            return false;
        }

        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("Database connection string not configured");
                return false;
            }
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var transaction = connection.BeginTransaction();
            try
            {
                const string selectQuery = @"
                    SELECT RequestID, CreatedDate 
                    FROM UserOTP 
                    WHERE RequestID = @RequestID 
                        AND otp = @OTP 
                        AND IsValidated = 0 
                        AND CreatedDate > DATEADD(MINUTE, -10, GETDATE())";
                        
                using var selectCommand = new SqlCommand(selectQuery, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };
                selectCommand.Parameters.Add("@RequestID", SqlDbType.NVarChar, 100).Value = customerId;
                selectCommand.Parameters.Add("@OTP", SqlDbType.NVarChar, 10).Value = secondFa;
                
                var result = await selectCommand.ExecuteScalarAsync();
                
                if (result != null)
                {
                    const string updateQuery = @"
                        UPDATE UserOTP 
                        SET IsValidated = 1, ValidatedDate = GETDATE() 
                        WHERE RequestID = @RequestID AND otp = @OTP AND IsValidated = 0";
                        
                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction) { CommandTimeout = CommandTimeoutSeconds };
                    updateCommand.Parameters.Add("@RequestID", SqlDbType.NVarChar, 100).Value = customerId;
                    updateCommand.Parameters.Add("@OTP", SqlDbType.NVarChar, 10).Value = secondFa;
                    
                    var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    
                    if (rowsAffected > 0)
                    {
                        await transaction.CommitAsync();
                        _logger.LogInformation("2FA validation successful for customer {CustomerId} using {SecondFaType}", customerId, secondFaType);
                        return true;
                    }
                }
                
                await transaction.RollbackAsync();
                _logger.LogWarning("2FA validation failed: Invalid or expired OTP for customer {CustomerId}", customerId);
                return false;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error during 2FA validation for customer {CustomerId}", customerId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating 2FA for customer {CustomerId}", customerId);
            return false;
        }
    }

    private string HashCustomerPin(string pin, string bvn)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(bvn))
            throw new ArgumentException("PIN and BVN cannot be null or empty");
            
        var combined = pin + bvn;
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(combined);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}