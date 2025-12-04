using KeyLoyalty.Application.DTOs;
using KeyLoyalty.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KeyLoyalty.API.Controllers
{
    [ApiController]
    [Route("api/loyalty")]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyApplicationService _loyaltyService;
        private readonly ILogger<LoyaltyController> _logger;

        public LoyaltyController(ILoyaltyApplicationService loyaltyService, ILogger<LoyaltyController> logger)
        {
            _loyaltyService = loyaltyService;
            _logger = logger;
        }

        [HttpGet("points/{userId}")]
        public async Task<ActionResult<LoyaltyDashboard>> GetDashboardByUserId(string userId)
        {
            _logger.LogInformation("🔥 INBOUND_REQUEST: GetDashboard called for userId={UserId} from OmniChannel at {Timestamp}", userId, DateTime.Now);
            _logger.LogInformation("📥 REQUEST_HEADERS: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}").ToArray()));
            
            try
            {
                _logger.LogInformation("🔄 PROCESSING: Fetching dashboard for user {UserId}", userId);
                var dashboard = await _loyaltyService.GetDashboardByUserIdAsync(userId);
                _logger.LogInformation("✅ SUCCESS: Retrieved dashboard for user {UserId} with {Points} points", userId, dashboard?.TotalPoints ?? 0);
                
                var response = new
                {
                    Dashboard = new
                    {
                        UserId = dashboard?.UserId,
                        AccountNumbers = dashboard?.AccountNumbers ?? new List<string>(),
                        TotalPoints = dashboard?.TotalPoints ?? 0,
                        Tier = dashboard?.Tier,
                        TierIcon = dashboard?.TierIcon,
                        PointsToNextTier = dashboard?.PointsToNextTier ?? 0,
                        PointsExpiryDate = dashboard?.PointsExpiryDate ?? DateTime.MinValue,
                        EarningPoints = dashboard?.EarningPoints ?? new List<EarningPoint>(),
                        TierPoints = dashboard?.TierPoints ?? new List<TierPoint>(),
                        TotalCashValue = (dashboard?.TotalPoints ?? 0) * 1.0m
                    },
                    TotalCashValue = (dashboard?.TotalPoints ?? 0) * 1.0m
                };
                
                return Ok(response);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "❌ DATABASE_ERROR: Failed to connect to database for user {UserId}", userId);
                return StatusCode(503, new { error = "Database connection failed", message = "Service temporarily unavailable" });
            }
           
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ VALIDATION_ERROR: Invalid input for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(new { error = "Invalid input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ INTERNAL_ERROR: Unexpected error for user {UserId}", userId);
                return StatusCode(500, new { error = "Internal server error"});
            }
        }

        [HttpGet("redeem-options")]
        public async Task<ActionResult<List<RedemptionOption>>> GetRedemptionOptions()
        {
            _logger.LogInformation("🔥 INBOUND_REQUEST: GetRedemptionOptions called from OmniChannel at {Timestamp}", DateTime.Now);
            _logger.LogInformation("📥 REQUEST_HEADERS: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}").ToArray()));
            
            try
            {
                _logger.LogInformation("🔄 PROCESSING: Fetching redemption options from service");
                var options = await _loyaltyService.GetRedemptionOptionsAsync();
                _logger.LogInformation("✅ SUCCESS: Retrieved {Count} redemption options", options?.Count ?? 0);
                return Ok(options);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "❌ DATABASE_ERROR: Failed to connect to database");
                return StatusCode(503, new { error = "Database connection failed", message = "Service temporarily unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ INTERNAL_ERROR: Unexpected error in GetRedemptionOptions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("redeem-points")]
        public async Task<ActionResult<RedemptionResponse>> RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            _logger.LogInformation("🔥 INBOUND_REQUEST: RedeemPoints called from OmniChannel at {Timestamp}", DateTime.Now);
            _logger.LogInformation("📥 REQUEST_DATA: Username={Username}, Points={Points}, Type={Type}", request?.Username, request?.PointsToRedeem, request?.RedemptionType);
            _logger.LogInformation("📥 REQUEST_HEADERS: {Headers}", string.Join(", ", Request.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value.ToArray())}").ToArray()));
            
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("⚠️ VALIDATION_ERROR: Request is null");
                    return BadRequest(new { error = "Invalid input", message = "Request body is required" });
                }
                
                _logger.LogInformation("🔄 PROCESSING: Redeeming {Points} points for user {Username}", request.PointsToRedeem, request.Username);
                var result = await _loyaltyService.RedeemPointsAsync(request);
                
                if (result.Success && !string.IsNullOrEmpty(request.RedemptionType))
                {
                    result.Message = request.RedemptionType switch
                    {
                        "AIRTIME" => $"₦{result.AmountRedeemed:N2} available for airtime purchase",
                        "BILL_PAYMENT" => $"₦{result.AmountRedeemed:N2} available for bill payment",
                        "TRANSFER" => $"₦{result.AmountRedeemed:N2} available for money transfer",
                        _ => result.Message
                    };
                }
                
                _logger.LogInformation("✅ SUCCESS: Redemption completed for user {Username}. Success={Success}, Amount={Amount}", request.Username, result.Success, result.AmountRedeemed);
                return Ok(result);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "❌ DATABASE_ERROR: Failed to connect to database for redemption");
                return StatusCode(503, new { error = "Database connection failed", message = "Service temporarily unavailable" });
            }
           
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ VALIDATION_ERROR: Invalid redemption request: {Message}", ex.Message);
                return BadRequest(new { error = "Invalid input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ INTERNAL_ERROR: Unexpected error during redemption");
                return StatusCode(500, new { error = "Internal server error"});
            }
        }



        [HttpPost("confirm-transaction")]
        public async Task<ActionResult<TransactionConfirmationResponse>> ConfirmTransaction([FromBody] TransactionConfirmationRequest request)
        {
            try
            {
                var result = await _loyaltyService.ConfirmTransactionAsync(request);
                return Ok(result);
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                return StatusCode(503, new { error = "Database connection failed", message = "Service temporarily unavailable" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "Invalid input", message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // Enhanced Features
        [HttpGet("dashboard/{accountNumber}")]
        public async Task<ActionResult> GetDashboard(string accountNumber)
        {
            try
            {
                var dashboard = await _loyaltyService.GetDashboardAsync(accountNumber);
                return Ok(dashboard);
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                return StatusCode(503, new { error = "Database connection failed", message = "Service temporarily unavailable" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "Invalid input", message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("admin/add-points/{userIdOrAccount}")]
        public async Task<ActionResult> AddPoints(string userIdOrAccount, [FromBody] AddPointsRequest request)
        {
            try
            {
                var points = await _loyaltyService.AssignPointsByUserIdAsync(userIdOrAccount, request.Points, "MANUAL_ADD");
                return Ok(new { success = true, pointsAdded = points, message = $"{points} points added to {userIdOrAccount}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding points to {UserIdOrAccount}", userIdOrAccount);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("admin/clear-points/{userId}")]
        public async Task<ActionResult> ClearPoints(string userId)
        {
            try
            {
                var success = await _loyaltyService.ResetUserPointsAsync(userId, 0);
                return Ok(new { success, message = success ? $"Points cleared for user {userId}" : "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing points for user {UserId}", userId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("reset-points/{userIdOrAccount}")]
        public async Task<ActionResult> ResetPoints(string userIdOrAccount, [FromBody] ResetPointsRequest request)
        {
            try
            {
                var success = await _loyaltyService.ResetPointsByUserOrAccountAsync(userIdOrAccount, request.Points);
                return Ok(new { success, message = success ? $"Points reset to {request.Points} for {userIdOrAccount}" : "User not found" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting points for {UserIdOrAccount}", userIdOrAccount);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("admin/reset-all-points")]
        public async Task<ActionResult> ResetAllPoints()
        {
            try
            {
                var count = await _loyaltyService.ResetAllUserPointsAsync();
                return Ok(new { success = true, usersReset = count, message = $"Points reset for {count} users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting all user points");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("check-loyalty-usage/{userIdOrAccount}/{transactionReference}")]
        public async Task<ActionResult> CheckLoyaltyUsage(string userIdOrAccount, string transactionReference)
        {
            try
            {
                var result = await _loyaltyService.CheckLoyaltyUsageAsync(userIdOrAccount, transactionReference);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking loyalty usage for {User} transaction {TxnRef}", userIdOrAccount, transactionReference);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("recent-transactions/{userIdOrAccount}")]
        public async Task<ActionResult> GetRecentTransactions(string userIdOrAccount)
        {
            try
            {
                var transactions = await _loyaltyService.GetRecentTransactionsAsync(userIdOrAccount);
                return Ok(transactions);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent transactions for {User}", userIdOrAccount);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("check-redemption-credit/{userIdOrAccount}/{transactionId}")]
        public async Task<ActionResult<LoyaltyRedemptionStatusDto>> CheckRedemptionCreditStatus(string userIdOrAccount, string transactionId)
        {
            _logger.LogInformation("Checking redemption credit status for {UserIdOrAccount}, Transaction: {TransactionId}", userIdOrAccount, transactionId);
            
            try
            {
                var result = await _loyaltyService.CheckRedemptionCreditStatusAsync(userIdOrAccount, transactionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking redemption credit status for {User} transaction {TxnId}", userIdOrAccount, transactionId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    public class AddPointsRequest
    {
        public int Points { get; set; }
    }

    public class ResetPointsRequest
    {
        public int Points { get; set; }
    }
}