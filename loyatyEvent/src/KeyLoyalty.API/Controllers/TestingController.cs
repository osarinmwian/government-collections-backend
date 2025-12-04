using Microsoft.AspNetCore.Mvc;
using KeyLoyalty.Application.Services;
using Microsoft.Data.SqlClient;

namespace KeyLoyalty.API.Controllers;

[ApiController]
[Route("api/testing")]
public class TestingController : ControllerBase
{
    private readonly ILoyaltyApplicationService _loyaltyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TestingController> _logger;

    public TestingController(ILoyaltyApplicationService loyaltyService, IConfiguration configuration, ILogger<TestingController> logger)
    {
        _loyaltyService = loyaltyService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("deduct-points/{userIdOrAccount}/{points}")]
    public async Task<IActionResult> TestPointDeduction(string userIdOrAccount, int points)
    {
        try
        {
            var result = await _loyaltyService.AssignPointsByUserIdAsync(userIdOrAccount, -points, "TEST_DEDUCTION");
            return Ok(new { success = true, pointsDeducted = Math.Abs(result), message = $"Deducted {Math.Abs(result)} points from {userIdOrAccount}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("gl-balance/{glAccount}")]
    public async Task<IActionResult> GetGLAccountBalance(string glAccount)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"SELECT 
                           GLAccount,
                           SUM(DebitAmount) as TotalDebits,
                           SUM(CreditAmount) as TotalCredits,
                           SUM(DebitAmount) - SUM(CreditAmount) as Balance,
                           COUNT(*) as TransactionCount
                       FROM AccountingEntries 
                       WHERE GLAccount = @GLAccount
                       GROUP BY GLAccount";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@GLAccount", glAccount);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Ok(new
                {
                    glAccount = reader.GetString(0),
                    totalDebits = reader.GetDecimal(1),
                    totalCredits = reader.GetDecimal(2),
                    balance = reader.GetDecimal(3),
                    transactionCount = reader.GetInt32(4)
                });
            }
            
            return Ok(new { glAccount, balance = 0m, message = "No transactions found for this GL account" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("gl-transactions/{glAccount}")]
    public async Task<IActionResult> GetGLAccountTransactions(string glAccount)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var sql = @"SELECT TOP 20 
                           TransactionId, GLAccount, DebitAmount, CreditAmount, 
                           Description, CreatedDate, AccountNumber
                       FROM AccountingEntries 
                       WHERE GLAccount = @GLAccount
                       ORDER BY CreatedDate DESC";
            
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@GLAccount", glAccount);
            
            var transactions = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(new
                {
                    transactionId = reader.GetString(0),
                    glAccount = reader.GetString(1),
                    debitAmount = reader.GetDecimal(2),
                    creditAmount = reader.GetDecimal(3),
                    description = reader.GetString(4),
                    createdDate = reader.GetDateTime(5),
                    accountNumber = reader.IsDBNull(6) ? "" : reader.GetString(6)
                });
            }
            
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("simulate-loyalty-transaction/{userIdOrAccount}/{points}")]
    public async Task<IActionResult> SimulateLoyaltyTransaction(string userIdOrAccount, int points)
    {
        try
        {
            var request = new KeyLoyalty.Application.DTOs.RedeemPointsRequest
            {
                AccountNumber = userIdOrAccount,
                PointsToRedeem = points,
                RedemptionType = "AIRTIME",
                Username = userIdOrAccount
            };
            
            var result = await _loyaltyService.RedeemPointsAsync(request);
            
            return Ok(new
            {
                success = result.Success,
                message = result.Message,
                pointsRedeemed = points,
                amountRedeemed = result.AmountRedeemed,
                remainingPoints = result.RemainingPoints,
                transactionId = result.TransactionId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("test-point-assignment/{userIdOrAccount}/{transactionType}/{amount}")]
    public async Task<IActionResult> TestPointAssignment(string userIdOrAccount, string transactionType, decimal amount)
    {
        try
        {
            var pointsAwarded = await _loyaltyService.AssignPointsAsync(userIdOrAccount, 0, transactionType, amount);
            
            return Ok(new
            {
                success = true,
                transactionType = transactionType,
                amount = amount,
                pointsAwarded = pointsAwarded,
                message = $"{transactionType} transaction of ₦{amount} awarded {pointsAwarded} points"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}