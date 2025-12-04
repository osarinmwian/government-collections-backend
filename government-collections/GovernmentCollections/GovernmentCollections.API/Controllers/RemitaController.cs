using GovernmentCollections.Domain.DTOs.Remita;
using GovernmentCollections.Service.Services.Remita;
using Microsoft.AspNetCore.Mvc;

namespace GovernmentCollections.API.Controllers;

[Route("api/v1/send/api/bgatesvc/v3/billpayment")]
[ApiController]
public class RemitaController : ControllerBase
{
    private readonly IRemitaService _remitaService;

    public RemitaController(IRemitaService remitaService)
    {
        _remitaService = remitaService;
    }

    [HttpGet("billers")]
    public async Task<IActionResult> GetBillers()
    {
        var result = await _remitaService.GetBillersAsync();
        return Ok(new { status = "00", message = "Request processed successfully", data = result });
    }

    [HttpGet("biller/{billerId}/products")]
    public async Task<IActionResult> GetBillerProducts(string billerId)
    {
        if (string.IsNullOrEmpty(billerId)) 
            return BadRequest(new { status = "01", message = "BillerId is required", data = (object?)null });
        
        var result = await _remitaService.GetBillerByIdAsync(billerId);
        return Ok(result);
    }

    [HttpPost("biller/customer/validation")]
    public async Task<IActionResult> ValidateCustomer([FromBody] RemitaValidateCustomerRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { status = "01", message = "Invalid request", data = (object?)null });
        
        var result = await _remitaService.ValidateCustomerAsync(request);
        return Ok(result);
    }

    [HttpPost("biller/transaction/process")]
    public async Task<IActionResult> ProcessTransaction([FromBody] RemitaTransactionInitiateDto request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (!ModelState.IsValid) 
            return BadRequest(new { status = "01", message = "Invalid request", data = ModelState });
        
        if (string.IsNullOrEmpty(request.Username))
            return BadRequest(new { status = "01", message = "Username is required", data = (object?)null });

        var result = await _remitaService.ProcessTransactionWithAuthAsync(request);
        return Ok(result);
    }

    [HttpPost("initiate-payment")]
    public async Task<IActionResult> InitiatePayment([FromBody] RemitaInitiatePaymentDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var result = await _remitaService.InitiatePaymentAsync(request);
        return Ok(result);
    }

    [HttpGet("verify-payment/{rrr}")]
    public async Task<IActionResult> VerifyPayment(string rrr)
    {
        if (string.IsNullOrEmpty(rrr)) 
            return BadRequest(new { Status = "ERROR", Message = "RRR is required" });
        
        var result = await _remitaService.VerifyPaymentAsync(rrr);
        return Ok(result);
    }

    [HttpGet("transaction-status/{transactionId}")]
    public async Task<IActionResult> TransactionStatus(string transactionId)
    {
        if (string.IsNullOrEmpty(transactionId)) 
            return BadRequest(new { Status = "ERROR", Message = "TransactionId is required" });
        
        var result = await _remitaService.GetTransactionStatusAsync(transactionId);
        return Ok(result);
    }

    [HttpGet("banks")]
    public async Task<IActionResult> GetActiveBanks()
    {
        var result = await _remitaService.GetActiveBanksAsync();
        return Ok(result);
    }

    [HttpPost("biller/activate/rrr/transaction/mandate")]
    public async Task<IActionResult> RrrActivateMandate([FromBody] RemitaRrrPaymentRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (string.IsNullOrEmpty(request.Rrr))
            return BadRequest(new { status = "01", message = "RRR is required", data = (object?)null });
        
        var result = await _remitaService.ActivateMandateAsync(request);
        return Ok(result);
    }

    [HttpGet("biller/transaction/lookup/{rrr}")]
    public async Task<IActionResult> GetRrrDetails(string rrr)
    {
        if (string.IsNullOrEmpty(rrr))
            return BadRequest(new { status = "01", message = "RRR is required", data = (object?)null });
        
        var result = await _remitaService.GetRrrDetailsAsync(rrr);
        return Ok(result);
    }

    [HttpPost("biller/rrr/transaction/payment")]
    public async Task<IActionResult> RrrTransactionPay([FromBody] RemitaRrrPaymentRequest request)
    {
        if (request == null)
            return BadRequest(new { status = "01", message = "Request body is required", data = (object?)null });
            
        if (string.IsNullOrEmpty(request.Rrr))
            return BadRequest(new { status = "01", message = "RRR is required", data = (object?)null });
        
        var result = await _remitaService.ProcessRrrPaymentAsync(request);
        return Ok(result);
    }
}