using GovernmentCollections.Data.Context;
using Microsoft.Data.SqlClient;

namespace GovernmentCollections.Service.Services.PinValidation;

public class PinValidationService : IPinValidationService
{
    private readonly IGovernmentCollectionsContext _context;

    public PinValidationService(IGovernmentCollectionsContext context)
    {
        _context = context;
    }

    public async Task<bool> ValidatePinAsync(string userId, string pin)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = "SELECT COUNT(1) FROM Users WHERE UserId = @UserId AND Pin = @Pin";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Pin", pin);
        
        var result = await command.ExecuteScalarAsync();
        var count = result != null ? (int)result : 0;
        return count > 0;
    }
}