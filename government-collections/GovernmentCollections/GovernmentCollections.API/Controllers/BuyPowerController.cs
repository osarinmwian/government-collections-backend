using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.DTOs.PinValidation;
using GovernmentCollections.Shared.Validation;
using GovernmentCollections.Service.Services.BuyPower;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GovernmentCollections.API.Controllers;

[Route("api/v1/buypower")]
public class BuyPowerController : BaseController
{
    private readonly ILogger<BuyPowerController> _logger;
    private readonly IBuyPowerService _buyPowerService;
    private readonly IPinValidationService _pinValidationService;

    public BuyPowerController(IBuyPowerService buyPowerService, ILogger<BuyPowerController> logger, IPinValidationService pinValidationService)
    {
        _logger = logger;
        _buyPowerService = buyPowerService;
        _pinValidationService = pinValidationService;
    }

    [HttpPost("payment")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentWithPinDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrEmpty(request.Pin)) return BadRequest(new { Status = "ERROR", Message = "PIN is required" });

        var userId = User.FindFirst("sub")?.Value ?? "";
        if (string.IsNullOrEmpty(userId)) return Unauthorized("User not authenticated");

        var isPinValid = await _pinValidationService.ValidatePinAsync(userId, request.Pin);
        if (!isPinValid) return Unauthorized(new { Status = "ERROR", Message = "Invalid PIN" });
        
        var result = await _buyPowerService.ProcessPaymentAsync(request);
        return result.Status switch
        {
            "SUCCESS" => Ok(result),
            "ERROR" => BadRequest(result),
            _ => StatusCode(500, result)
        };
    }
}