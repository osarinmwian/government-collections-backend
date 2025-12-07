using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace GovernmentCollections.Service.Services.Remita.Validation;

public class PinValidationService : IPinValidationService
{
    private readonly IConfiguration _configuration;

    public PinValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> ValidatePinAsync(string userId, string pin)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = "SELECT TransactionPin FROM OmniProfiles WHERE UserId = @UserId";
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        
        var storedPin = await command.ExecuteScalarAsync() as string;
        
        if (string.IsNullOrEmpty(storedPin))
            return false;
            
        return VerifyPin(pin, storedPin);
    }

    public async Task<bool> Validate2FAAsync(string userId, string secondFa, string secondFaType)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = secondFaType.ToLower() switch
        {
            "sms" => "SELECT PhoneNumber FROM OmniProfiles WHERE UserId = @UserId",
            "email" => "SELECT Email FROM OmniProfiles WHERE UserId = @UserId",
            "token" => "SELECT Token FROM OmniProfiles WHERE UserId = @UserId",
            _ => throw new ArgumentException("Invalid 2FA type")
        };
        
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        
        var storedValue = await command.ExecuteScalarAsync() as string;
        
        return secondFaType.ToLower() switch
        {
            "sms" => ValidateSmsOtp(secondFa, storedValue),
            "email" => ValidateEmailOtp(secondFa, storedValue),
            "token" => ValidateToken(secondFa, storedValue),
            _ => false
        };
    }

    private bool VerifyPin(string inputPin, string storedPin)
    {
        return ComputeHash(inputPin) == storedPin;
    }
    
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool ValidateSmsOtp(string otp, string? phoneNumber)
    {
        return !string.IsNullOrEmpty(phoneNumber) && otp.Length == 6 && otp.All(char.IsDigit);
    }

    private bool ValidateEmailOtp(string otp, string? email)
    {
        return !string.IsNullOrEmpty(email) && otp.Length == 6 && otp.All(char.IsDigit);
    }

    private bool ValidateToken(string token, string? storedToken)
    {
        return !string.IsNullOrEmpty(storedToken) && token == storedToken;
    }
}