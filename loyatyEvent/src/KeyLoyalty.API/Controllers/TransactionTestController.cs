// using KeyLoyalty.Application.Services;
// using KeyLoyalty.Application.DTOs;
// using Microsoft.AspNetCore.Mvc;

// namespace KeyLoyalty.API.Controllers;

// [ApiController]
// [Route("api/transactions")]
// public class TransactionController : ControllerBase
// {
//     private readonly ILoyaltyApplicationService _loyaltyService;
//     private readonly ILogger<TransactionController> _logger;

//     public TransactionController(ILoyaltyApplicationService loyaltyService, ILogger<TransactionController> logger)
//     {
//         _loyaltyService = loyaltyService;
//         _logger = logger;
//     }

//     [HttpPost("process")]
//     public async Task<ActionResult> ProcessTransaction([FromBody] TransactionProcessRequest request)
//     {
//         try
//         {
//             // Validate transaction type
//             var validTypes = new[] { "AIRTIME_PURCHASE", "BILL_PAYMENT", "NIP_TRANSFER" };
//             if (!validTypes.Contains(request.TransactionType))
//             {
//                 return BadRequest($"Invalid transaction type '{request.TransactionType}'. Valid types are: {string.Join(", ", validTypes)}");
//             }

//             await _loyaltyService.AssignPointsAsync(request.AccountNumber, 0, request.TransactionType, request.Amount);
            
//             var message = request.TransactionType switch
//             {
//                 "AIRTIME_PURCHASE" => $"Airtime purchase of ₦{request.Amount} processed",
//                 "BILL_PAYMENT" => $"Bill payment of ₦{request.Amount} processed",
//                 "NIP_TRANSFER" => $"NIP transfer of ₦{request.Amount} processed",
//                 _ => $"Transaction of ₦{request.Amount} processed"
//             };
            
//             return Ok(new { 
//                 Message = message,
//                 TransactionType = request.TransactionType
//             });
//         }
//         catch (Exception ex)
//         {
//             return BadRequest(ex?.Message);
//         }
//     }

//     [HttpPost("test-all")]
//     public async Task<ActionResult> RunAllTests()
//     {
//         var results = new List<object>();
//         var testAccount = "1006817382";
//         var testUserId = "oluwajoba";

//         try
//         {
//             // Test 1: AIRTIME_PURCHASE
//             await _loyaltyService.AssignPointsAsync(testAccount, 0, "AIRTIME_PURCHASE", 2000);
//             results.Add(new { Test = "AIRTIME_PURCHASE", Amount = 2000, ExpectedPoints = 1, Status = "✅ Completed" });

//             // Test 2: BILL_PAYMENT  
//             await _loyaltyService.AssignPointsAsync(testAccount, 0, "BILL_PAYMENT", 1500);
//             results.Add(new { Test = "BILL_PAYMENT", Amount = 1500, ExpectedPoints = 3, Status = "✅ Completed" });

//             // Test 3: NIP_TRANSFER
//             await _loyaltyService.AssignPointsAsync(testAccount, 0, "NIP_TRANSFER", 5000);
//             results.Add(new { Test = "NIP_TRANSFER", Amount = 5000, ExpectedPoints = 2, Status = "✅ Completed" });

//             // Test 4: Get Dashboard
//             var dashboard = await _loyaltyService.GetDashboardByUserIdAsync(testUserId);
//             results.Add(new { Test = "DASHBOARD", Dashboard = dashboard, Status = "✅ Retrieved" });

//             return Ok(new
//             {
//                 Message = "All tests completed successfully",
//                 TestAccount = testAccount,
//                 TestUserId = testUserId,
//                 ExpectedFlow = "1 + 3 + 2 = 6 points total",
//                 Results = results
//             });
//         }
//         catch (Exception ex)
//         {
//             return BadRequest(new
//             {
//                 Message = "Test failed",
//                 Error = ex.Message,
//                 CompletedTests = results
//             });
//         }
//     }

// }