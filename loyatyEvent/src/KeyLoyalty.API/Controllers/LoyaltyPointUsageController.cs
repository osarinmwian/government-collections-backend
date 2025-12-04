using Microsoft.AspNetCore.Mvc;
using KeyLoyalty.Application.Services;
using KeyLoyalty.Application.DTOs;

namespace KeyLoyalty.API.Controllers;

[ApiController]
[Route("api/loyalty")]
public class LoyaltyPointUsageController : ControllerBase
{
    private readonly ILoyaltyApplicationService _loyaltyService;
    private readonly ILogger<LoyaltyPointUsageController> _logger;

    public LoyaltyPointUsageController(ILoyaltyApplicationService loyaltyService, ILogger<LoyaltyPointUsageController> logger)
    {
        _loyaltyService = loyaltyService;
        _logger = logger;
    }

    [HttpPost("use-points")]
    public async Task<IActionResult> UsePointsForTransaction([FromBody] UsePointsRequest request)
    {
        try
        {
            var result = await _loyaltyService.UsePointsForTransactionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using points for transaction");
            return BadRequest(new { message = ex.Message });
        }
    }
}