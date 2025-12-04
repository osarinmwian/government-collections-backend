using KeyLoyalty.Application.Services;
using KeyLoyalty.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace KeyLoyalty.API.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly ILoyaltyApplicationService _loyaltyService;
    private readonly ILogger<TestController> _logger;

    public TestController(ILoyaltyApplicationService loyaltyService, ILogger<TestController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    [HttpPost("add-transaction")]
    public async Task<ActionResult> AddTransaction([FromBody] AddTransactionRequest request)
    {
        try
        {
            var points = await _loyaltyService.AssignPointsAsync(request.AccountNumber, 0, request.TransactionType, request.Amount);
            
            return Ok(new
            {
                Message = "Transaction processed successfully",
                AccountNumber = request.AccountNumber,
                TransactionType = request.TransactionType,
                Amount = request.Amount,
                PointsAwarded = points
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = "Transaction failed",
                Error = ex.Message
            });
        }
    }

    [HttpPost("clear-points/{accountNumber}")]
    public async Task<ActionResult> ClearPoints(string accountNumber)
    {
        try
        {
            await _loyaltyService.AssignPointsAsync(accountNumber, -1000000, "CLEAR_POINTS", 0);
            
            return Ok(new
            {
                Message = "Points cleared successfully",
                AccountNumber = accountNumber
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = "Clear points failed",
                Error = ex.Message
            });
        }
    }

    // [HttpGet("dashboard/{userId}")]
    // public async Task<ActionResult> GetTestDashboard(string userId)
    // {
    //     try
    //     {
    //         var dashboard = await _loyaltyService.GetDashboardByUserIdAsync(userId);
    //         return Ok(new
    //         {
    //             Message = $"Dashboard for user: {userId}",
    //             Dashboard = dashboard
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex.Message);
    //     }
    // }

    // [HttpPost("single-transaction")]
    // public async Task<ActionResult> TestSingleTransaction([FromBody] TransactionProcessRequest request)
    // {
    //     try
    //     {
    //         var points = await _loyaltyService.AssignPointsAsync(request.AccountNumber, 0, request.TransactionType, request.Amount);

    //         return Ok(new
    //         {
    //             Message = "Transaction processed",
    //             AccountNumber = request.AccountNumber,
    //             TransactionType = request.TransactionType,
    //             Amount = request.Amount,
    //             PointsAwarded = points
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(ex.Message);
    //     }
    // }
}

public class AddTransactionRequest
{
    public string AccountNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}