using GovernmentCollections.Domain.DTOs.RevPay;
using GovernmentCollections.Service.Services.RevPay;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentCollections.API.Controllers;

[Route("api/v1/revpay")]
[ApiController]
public class RevPayController : ControllerBase
{
    private readonly IRevPayService _revPayService;

    public RevPayController(IRevPayService revPayService)
    {
        _revPayService = revPayService;
    }

    [HttpPost("billtypes")]
    public async Task<IActionResult> GetBillTypes()
    {
        var result = await _revPayService.GetBillTypesAsync();
        return Ok(result);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateReference([FromBody] RevPayValidateRequest request)
    {
        if (!ModelState.IsValid) 
            return BadRequest(new { status = "01", message = "Invalid request", data = (object?)null });
        
        var result = await _revPayService.ValidateReferenceAsync(request);
        return Ok(result);
    }

    [HttpPost("payment")]
    public async Task<IActionResult> ProcessPayment([FromBody] RevPayPaymentRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (!ModelState.IsValid) 
            return BadRequest(new { status = "01", message = "Invalid request", data = ModelState });
        
        var result = await _revPayService.ProcessPaymentAsync(request);
        return Ok(result);
    }

    [HttpPost("webguid")]
    public async Task<IActionResult> GenerateWebGuid([FromBody] RevPayWebGuidRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (!ModelState.IsValid) 
            return BadRequest(new { status = "01", message = "Invalid request", data = ModelState });
        
        var result = await _revPayService.GenerateWebGuidAsync(request);
        return Ok(result);
    }

    [HttpPost("verify-pid")]
    public async Task<IActionResult> VerifyPid([FromBody] RevPayPidVerificationRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (string.IsNullOrEmpty(request.Pid))
            return BadRequest(new { status = "01", message = "PID is required", data = (object?)null });
        
        var result = await _revPayService.VerifyPidAsync(request);
        return Ok(result);
    }

    [HttpPost("receipt")]
    public async Task<IActionResult> GetReceipt([FromBody] RevPayReceiptRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (string.IsNullOrEmpty(request.PaymentRef))
            return BadRequest(new { status = "01", message = "PaymentRef is required", data = (object?)null });
        
        var result = await _revPayService.GetReceiptAsync(request);
        return Ok(result);
    }

    [HttpPost("process-transaction")]
    public async Task<IActionResult> ProcessTransaction([FromBody] RevPayTransactionRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (!ModelState.IsValid) 
            return BadRequest(new { status = "01", message = "Invalid request", data = ModelState });
        
        if (string.IsNullOrEmpty(request.AccountNumber))
            return BadRequest(new { status = "01", message = "AccountNumber is required", data = (object?)null });

        var result = await _revPayService.ProcessTransactionWithAuthAsync(request);
        return Ok(result);
    }
}