using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentCollections.API.Controllers;

[ApiController]
// [Authorize] // Temporarily disabled for testing
public abstract class BaseController : ControllerBase
{

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



}