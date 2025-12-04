using KeyLoyalty.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace KeyLoyalty.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly ILoyaltyLogService _logService;
        private readonly ILogger<LogsController> _logger;

        public LogsController(ILoyaltyLogService logService, ILogger<LogsController> logger)
        {
            _logService = logService;
            _logger = logger;
        }

        [HttpGet("inbound")]
        public async Task<IActionResult> GetInboundLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var logs = await _logService.GetInboundLogsAsync(fromDate, toDate, limit);
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving inbound logs");
                return StatusCode(500, new { success = false, message = "Error retrieving inbound logs" });
            }
        }

        [HttpGet("outbound")]
        public async Task<IActionResult> GetOutboundLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var logs = await _logService.GetOutboundLogsAsync(fromDate, toDate, limit);
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving outbound logs");
                return StatusCode(500, new { success = false, message = "Error retrieving outbound logs" });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var logs = await _logService.GetAllLogsAsync(fromDate, toDate, limit);
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all logs");
                return StatusCode(500, new { success = false, message = "Error retrieving all logs" });
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestLogging([FromBody] object testData)
        {
            try
            {
                await _logService.LogInboundRequestAsync("POST", "/api/logs/test", 
                    Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), 
                    testData);
                
                var response = new { message = "Test log entry created", timestamp = DateTime.UtcNow };
                
                await _logService.LogOutboundResponseAsync(200, 
                    Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), 
                    response);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test logging");
                return StatusCode(500, new { success = false, message = "Error in test logging" });
            }
        }
    }
}