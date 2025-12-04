using GovernmentCollections.Domain.DTOs;
using GovernmentCollections.Domain.DTOs.PinValidation;
using GovernmentCollections.Service.Services.PinValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentCollections.API.Controllers;

[ApiController]
// [Authorize] // Temporarily disabled for testing
public abstract class BaseController : ControllerBase
{
    protected readonly IPinValidationService _pinService;

    protected BaseController(IPinValidationService pinService)
    {
        _pinService = pinService;
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = new[] { "Tax", "Levy", "License", "StatutoryFee", "VehicleLicense", "BusinessPermit" };
        return Ok(categories);
    }

    [HttpGet("list/{category}")]
    public IActionResult GetListByCategory(string category)
    {
        var items = new[] { $"{category}_Item1", $"{category}_Item2" };
        return Ok(items);
    }

    [HttpPost("validate-customer")]
    public IActionResult ValidateCustomer([FromBody] object request)
    {
        return Ok(new { IsValid = true, Message = "Customer validated" });
    }

    protected async Task<bool> ValidatePinAsync(string userId, string pin)
    {
        return await _pinService.ValidatePinAsync(userId, pin);
    }
}