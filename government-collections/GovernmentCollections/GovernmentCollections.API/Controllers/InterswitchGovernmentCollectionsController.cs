using GovernmentCollections.Domain.DTOs.Interswitch;
using GovernmentCollections.Service.Services.InterswitchGovernmentCollections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GovernmentCollections.API.Controllers;

// [Authorize] // Temporarily disabled for testing
[ApiController]
[Route("api/v1/interswitch/")]
public class InterswitchGovernmentCollectionsController : BaseController
{
    private readonly IInterswitchGovernmentCollectionsService _interswitchService;
    private readonly ILogger<InterswitchGovernmentCollectionsController> _logger;

    public InterswitchGovernmentCollectionsController(
        IInterswitchGovernmentCollectionsService interswitchService,
        ILogger<InterswitchGovernmentCollectionsController> logger)
    {
        _interswitchService = interswitchService;
        _logger = logger;
    }

    [HttpGet("government-collections/billers")]
    public async Task<IActionResult> GetGovernmentBillers()
    {
        var billers = await _interswitchService.GetGovernmentBillersAsync();
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Government billers retrieved successfully",
            Data = billers,
            Count = billers.Count
        });
    }



    [HttpGet("government-collections/billers/{categoryId}")]
    public async Task<IActionResult> GetBillersByCategory(int categoryId)
    {
        var billers = await _interswitchService.GetBillersByCategoryAsync(categoryId);
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Billers retrieved successfully",
            Data = billers,
            Count = billers.Count
        });
    }

    [HttpGet("government-collections/categories")]
    public async Task<IActionResult> GetGovernmentCategories()
    {
        var categories = await _interswitchService.GetGovernmentCategoriesAsync();
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Government categories retrieved successfully",
            Data = categories,
            Count = categories.Count
        });
    }



    [HttpGet("transaction-history")]
    public async Task<IActionResult> GetTransactionHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst("sub")?.Value ?? "";
        
        var history = await _interswitchService.GetTransactionHistoryAsync(userId, page, pageSize);
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Transaction history retrieved successfully",
            Data = history
        });
    }

    [HttpGet("service-options/{serviceId}")]
    public async Task<IActionResult> GetServiceOptions(int serviceId)
    {
        var paymentItems = await _interswitchService.GetServiceOptionsAsync(serviceId);
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Service options retrieved successfully",
            Data = paymentItems,
            Count = paymentItems.Count
        });
    }

    [HttpPost("process-transaction")]
    public async Task<IActionResult> ProcessTransaction([FromBody] InterswitchTransactionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _interswitchService.ProcessTransactionAsync(request);
        
        if (result.ResponseCode == "00")
        {
            return Ok(new { 
                Status = "SUCCESS", 
                Message = "Transaction processed successfully",
                Data = result
            });
        }
        
        return BadRequest(new { 
            Status = "ERROR", 
            Message = result.ResponseMessage ?? "Transaction processing failed",
            Code = result.ResponseCode
        });
    }

    [HttpGet("transaction-status/{requestReference}")]
    public async Task<IActionResult> GetTransactionStatus(string requestReference)
    {
        if (string.IsNullOrEmpty(requestReference))
            return BadRequest(new { Status = "ERROR", Message = "Request reference is required" });

        var result = await _interswitchService.GetTransactionStatusAsync(requestReference);
        
        return Ok(new { 
            Status = "SUCCESS", 
            Message = "Transaction status retrieved successfully",
            Data = result
        });
    }



}