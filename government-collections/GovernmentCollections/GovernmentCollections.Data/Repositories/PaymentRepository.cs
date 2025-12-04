using GovernmentCollections.Data.Context;
using GovernmentCollections.Domain.Entities;
using GovernmentCollections.Domain.Enums;
using Microsoft.Data.SqlClient;

namespace GovernmentCollections.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IGovernmentCollectionsContext _context;

    public PaymentRepository(IGovernmentCollectionsContext context)
    {
        _context = context;
    }

    public async Task<GovernmentPayment> CreateAsync(GovernmentPayment payment)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = @"INSERT INTO GovernmentPayments 
                   (TransactionReference, CustomerReference, PayerName, PayerEmail, PayerPhone, 
                    PaymentType, Gateway, Amount, Description, Status, GatewayReference, 
                    GatewayResponse, CreatedAt, Channel, UserId)
                   VALUES (@TransactionReference, @CustomerReference, @PayerName, @PayerEmail, @PayerPhone,
                          @PaymentType, @Gateway, @Amount, @Description, @Status, @GatewayReference,
                          @GatewayResponse, @CreatedAt, @Channel, @UserId);
                   SELECT CAST(SCOPE_IDENTITY() as int)";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionReference", payment.TransactionReference);
        command.Parameters.AddWithValue("@CustomerReference", payment.CustomerReference);
        command.Parameters.AddWithValue("@PayerName", payment.PayerName);
        command.Parameters.AddWithValue("@PayerEmail", payment.PayerEmail);
        command.Parameters.AddWithValue("@PayerPhone", payment.PayerPhone);
        command.Parameters.AddWithValue("@PaymentType", (int)payment.PaymentType);
        command.Parameters.AddWithValue("@Gateway", (int)payment.Gateway);
        command.Parameters.AddWithValue("@Amount", payment.Amount);
        command.Parameters.AddWithValue("@Description", payment.Description);
        command.Parameters.AddWithValue("@Status", (int)payment.Status);
        command.Parameters.AddWithValue("@GatewayReference", payment.GatewayReference);
        command.Parameters.AddWithValue("@GatewayResponse", payment.GatewayResponse);
        command.Parameters.AddWithValue("@CreatedAt", payment.CreatedAt);
        command.Parameters.AddWithValue("@Channel", payment.Channel);
        command.Parameters.AddWithValue("@UserId", payment.UserId);
        
        var result = await command.ExecuteScalarAsync();
        payment.Id = result != null ? (int)result : 0;
        return payment;
    }

    public async Task<GovernmentPayment?> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out int intId)) return null;

        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM GovernmentPayments WHERE Id = @Id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", intId);
        
        using var reader = await command.ExecuteReaderAsync();
        return reader.Read() ? MapToPayment(reader) : null;
    }

    public async Task<GovernmentPayment?> GetByTransactionReferenceAsync(string transactionReference)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM GovernmentPayments WHERE TransactionReference = @TransactionReference";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionReference", transactionReference);
        
        using var reader = await command.ExecuteReaderAsync();
        return reader.Read() ? MapToPayment(reader) : null;
    }

    public async Task<List<GovernmentPayment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var offset = (page - 1) * pageSize;
        var sql = @"SELECT * FROM GovernmentPayments 
                   WHERE UserId = @UserId 
                   ORDER BY CreatedAt DESC 
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Offset", offset);
        command.Parameters.AddWithValue("@PageSize", pageSize);
        
        var payments = new List<GovernmentPayment>();
        using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            payments.Add(MapToPayment(reader));
        }
        return payments;
    }

    public async Task<GovernmentPayment> UpdateAsync(GovernmentPayment payment)
    {
        payment.UpdatedAt = DateTime.UtcNow;
        
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = @"UPDATE GovernmentPayments SET 
                   Status = @Status, GatewayReference = @GatewayReference, 
                   GatewayResponse = @GatewayResponse, UpdatedAt = @UpdatedAt
                   WHERE Id = @Id";
        
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Status", (int)payment.Status);
        command.Parameters.AddWithValue("@GatewayReference", payment.GatewayReference);
        command.Parameters.AddWithValue("@GatewayResponse", payment.GatewayResponse);
        command.Parameters.AddWithValue("@UpdatedAt", payment.UpdatedAt);
        command.Parameters.AddWithValue("@Id", payment.Id);
        
        await command.ExecuteNonQueryAsync();
        return payment;
    }

    public async Task<List<GovernmentPayment>> GetByStatusAsync(TransactionStatus status)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = "SELECT * FROM GovernmentPayments WHERE Status = @Status";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Status", (int)status);
        
        var payments = new List<GovernmentPayment>();
        using var reader = await command.ExecuteReaderAsync();
        while (reader.Read())
        {
            payments.Add(MapToPayment(reader));
        }
        return payments;
    }

    public async Task<bool> ExistsAsync(string transactionReference)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        
        var sql = "SELECT COUNT(1) FROM GovernmentPayments WHERE TransactionReference = @TransactionReference";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TransactionReference", transactionReference);
        
        var result = await command.ExecuteScalarAsync();
        var count = result != null ? (int)result : 0;
        return count > 0;
    }

    private GovernmentPayment MapToPayment(SqlDataReader reader)
    {
        return new GovernmentPayment
        {
            Id = reader.GetInt32(0),
            TransactionReference = reader.GetString(1),
            CustomerReference = reader.GetString(2),
            PayerName = reader.GetString(3),
            PayerEmail = reader.GetString(4),
            PayerPhone = reader.GetString(5),
            PaymentType = (PaymentType)reader.GetInt32(6),
            Gateway = (PaymentGateway)reader.GetInt32(7),
            Amount = reader.GetDecimal(8),
            Description = reader.GetString(9),
            Status = (TransactionStatus)reader.GetInt32(10),
            GatewayReference = reader.GetString(11),
            GatewayResponse = reader.GetString(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
            Channel = reader.GetString(15),
            UserId = reader.GetString(16)
        };
    }
}