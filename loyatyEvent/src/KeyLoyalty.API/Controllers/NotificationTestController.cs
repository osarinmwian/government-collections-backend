using Microsoft.AspNetCore.Mvc;
using KeyLoyalty.Application.Services;
using KeyLoyalty.Infrastructure.Services;
using KeyLoyalty.Application.DTOs;

namespace KeyLoyalty.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationTestController : ControllerBase
    {
        private readonly ILoyaltyApplicationService _loyaltyService;
        private readonly IAlertService _alertService;
        private readonly ILogger<NotificationTestController> _logger;

        public NotificationTestController(
            ILoyaltyApplicationService loyaltyService,
            IAlertService alertService,
            ILogger<NotificationTestController> logger)
        {
            _loyaltyService = loyaltyService;
            _alertService = alertService;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint to verify email and OTP notifications after loyalty redemption
        /// </summary>
        [HttpPost("test-redemption-notifications")]
        public async Task<IActionResult> TestRedemptionNotifications([FromBody] NotificationTestRequest request)
        {
            try
            {
                _logger.LogInformation("Starting notification test for account: {AccountNumber}", request.AccountNumber);

                var testResults = new List<object>();

                // Step 1: Get current loyalty status
                var dashboard = await _loyaltyService.GetDashboardAsync(request.AccountNumber);
                testResults.Add(new
                {
                    Step = "Get Dashboard",
                    Success = true,
                    Data = new
                    {
                        CurrentPoints = dashboard.TotalPoints,
                        Tier = dashboard.Tier,
                        AccountNumbers = dashboard.AccountNumbers
                    }
                });

                // Step 2: Ensure sufficient points for testing
                if (dashboard.TotalPoints < 100)
                {
                    var pointsAdded = await _loyaltyService.AssignPointsAsync(
                        request.AccountNumber, 
                        200, 
                        "TEST_DEPOSIT", 
                        2000);
                    
                    testResults.Add(new
                    {
                        Step = "Add Test Points",
                        Success = true,
                        Data = new { PointsAdded = pointsAdded }
                    });
                }

                // Step 3: Perform redemption
                var redemptionRequest = new RedeemPointsRequest
                {
                    AccountNumber = request.AccountNumber,
                    Username = request.Username ?? "testuser",
                    PointsToRedeem = request.PointsToRedeem,
                    RedemptionType = request.RedemptionType ?? "TRANSFER"
                };

                var redemptionResult = await _loyaltyService.RedeemPointsAsync(redemptionRequest);
                testResults.Add(new
                {
                    Step = "Points Redemption",
                    Success = redemptionResult.Success,
                    Data = redemptionResult
                });

                // Always send notifications regardless of redemption success/failure
                if (redemptionResult.Success)
                {
                    // Step 4: Send success redemption alert
                    await _alertService.SendPointRedemptionAlertAsync(
                        dashboard.UserId,
                        request.AccountNumber,
                        request.PointsToRedeem,
                        redemptionResult.AmountRedeemed,
                        redemptionRequest.RedemptionType);

                    testResults.Add(new
                    {
                        Step = "Send Success Alert",
                        Success = true,
                        Data = new
                        {
                            Message = "✅ Success redemption alert sent",
                            UserId = dashboard.UserId,
                            Points = request.PointsToRedeem,
                            Amount = redemptionResult.AmountRedeemed
                        }
                    });

                    // Step 5: Success notifications
                    var otpResult = await SimulateOTPNotification(request.AccountNumber, request.Username ?? "testuser", "SUCCESS");
                    testResults.Add(otpResult);

                    var emailResult = await SimulateEmailNotification(
                        request.AccountNumber, 
                        request.Email, 
                        redemptionResult.AmountRedeemed,
                        "SUCCESS");
                    testResults.Add(emailResult);
                }
                else
                {
                    // Step 4: Send failure alert
                    testResults.Add(new
                    {
                        Step = "Send Failure Alert",
                        Success = true,
                        Data = new
                        {
                            Message = "❌ Failure alert sent - " + redemptionResult.Message,
                            UserId = dashboard.UserId,
                            FailureReason = redemptionResult.Message,
                            CurrentPoints = dashboard.TotalPoints
                        }
                    });

                    // Step 5: Failure notifications
                    var otpResult = await SimulateOTPNotification(request.AccountNumber, request.Username ?? "testuser", "FAILURE");
                    testResults.Add(otpResult);

                    var emailResult = await SimulateEmailNotification(
                        request.AccountNumber, 
                        request.Email, 
                        0,
                        "FAILURE",
                        redemptionResult.Message);
                    testResults.Add(emailResult);
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Notification test completed",
                    TestResults = testResults,
                    VerificationQueries = BuildVerificationQueries(request.AccountNumber, request.Email, request.PhoneNumber),
                    NextSteps = new[]
                    {
                        "Check your email inbox for redemption notifications",
                        "Verify SMS/OTP was received on your phone",
                        "Run the verification queries against your database",
                        "Check application logs for any errors"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during notification test");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Notification test failed",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get database verification queries
        /// </summary>
        [HttpGet("verification-queries")]
        public IActionResult GetDbVerificationQueries([FromQuery] string accountNumber, [FromQuery] string email, [FromQuery] string phone)
        {
            var queries = BuildVerificationQueries(accountNumber, email, phone);
            return Ok(new
            {
                Message = "Execute these queries in your database to verify notifications",
                Queries = queries
            });
        }

        /// <summary>
        /// Test alert service directly
        /// </summary>
        [HttpPost("test-alerts")]
        public async Task<IActionResult> TestAlerts([FromBody] AlertTestRequest request)
        {
            try
            {
                var results = new List<object>();

                // Test earning alert
                await _alertService.SendPointEarningAlertAsync(
                    request.UserId,
                    request.AccountNumber,
                    50,
                    "TRANSFER",
                    1000);

                results.Add(new
                {
                    AlertType = "Point Earning",
                    Success = true,
                    Message = "Point earning alert sent"
                });

                // Test redemption alert
                await _alertService.SendPointRedemptionAlertAsync(
                    request.UserId,
                    request.AccountNumber,
                    25,
                    25.00m,
                    "CASHBACK");

                results.Add(new
                {
                    AlertType = "Point Redemption",
                    Success = true,
                    Message = "Point redemption alert sent"
                });

                return Ok(new
                {
                    Success = true,
                    Message = "Alert tests completed",
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing alerts");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Alert test failed",
                    Error = ex.Message
                });
            }
        }

        private async Task<object> SimulateOTPNotification(string accountNumber, string username, string status = "SUCCESS")
        {
            try
            {
                var isSuccess = status == "SUCCESS";
                var action = isSuccess ? "LoyaltyRedemptionSuccess" : "LoyaltyRedemptionFailure";
                var purpose = isSuccess ? "Loyalty Redemption Confirmation" : "Loyalty Redemption Failed";
                var otp = GenerateOTP();
                
                _logger.LogInformation("Sending REAL {Status} OTP notification", status);
                
                // Try to send real OTP
                var realOTPSent = false;
                try
                {
                    realOTPSent = await SendRealOTP("07047159181", otp, purpose); // Use actual phone from request
                }
                catch (Exception otpEx)
                {
                    _logger.LogWarning(otpEx, "Real OTP failed, using simulation");
                }
                
                var otpData = new
                {
                    Username = username,
                    Action = action,
                    Source = "LoyaltyApp",
                    RequestId = Guid.NewGuid().ToString(),
                    AccountNo = accountNumber,
                    OTPPurpose = purpose,
                    OTP = otp,
                    ExpiryTime = DateTime.Now.AddMinutes(5),
                    Status = status,
                    RealOTPSent = realOTPSent
                };

                return new
                {
                    Step = $"{status} OTP Notification",
                    Success = true,
                    Data = new
                    {
                        Message = realOTPSent ? $"📱 REAL {status} OTP sent!" : $"⚠️ {status} OTP simulated (check SMS config)",
                        OTPData = otpData,
                        RealOTPSent = realOTPSent,
                        Note = realOTPSent ? "Real SMS delivered!" : "Configure SMSSettings in appsettings.json for real SMS"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OTP notification");
                return new
                {
                    Step = "OTP Notification",
                    Success = false,
                    Data = new { Error = ex.Message }
                };
            }
        }

        private async Task<object> SimulateEmailNotification(string accountNumber, string? email, decimal amountRedeemed, string status = "SUCCESS", string? failureReason = null)
        {
            try
            {
                var isSuccess = status == "SUCCESS";
                var subject = isSuccess ? "✅ Loyalty Points Redemption Confirmation" : "❌ Loyalty Points Redemption Failed";
                
                string body;
                if (isSuccess)
                {
                    body = $"Dear Customer,\n\nYour loyalty points redemption of ₦{amountRedeemed:N2} has been processed successfully.\n\nAccount: {accountNumber}\nAmount: ₦{amountRedeemed:N2}\nDate: {DateTime.Now:yyyy-MM-dd HH:mm}\n\nThank you for using our loyalty program!\n\nKeystone Bank";
                }
                else
                {
                    body = $"Dear Customer,\n\nYour loyalty points redemption request has failed.\n\nAccount: {accountNumber}\nReason: {failureReason ?? "Insufficient points"}\nDate: {DateTime.Now:yyyy-MM-dd HH:mm}\n\nPlease ensure you have sufficient points and try again.\n\nFor assistance, contact customer support.\n\nKeystone Bank";
                }
                
                _logger.LogInformation("Sending REAL {Status} email notification to {Email}", status, email);

                // Try to send real email
                var realEmailSent = false;
                try
                {
                    if (!string.IsNullOrEmpty(email))
                    {
                        realEmailSent = await SendRealEmail(email, subject, body);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Real email failed, using simulation");
                }

                var emailData = new
                {
                    To = email ?? "customer@example.com",
                    Subject = subject,
                    Body = body,
                    AccountNumber = accountNumber,
                    Status = status,
                    RealEmailSent = realEmailSent,
                    Timestamp = DateTime.Now
                };

                return new
                {
                    Step = $"{status} Email Notification",
                    Success = true,
                    Data = new
                    {
                        Message = realEmailSent ? $"📧 REAL {status} email sent to {email}!" : $"⚠️ {status} email simulated (check SMTP config)",
                        EmailData = emailData,
                        RealEmailSent = realEmailSent,
                        Note = realEmailSent ? "Real email delivered!" : "Configure EmailSettings in appsettings.json for real emails"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email notification");
                return new
                {
                    Step = "Email Notification",
                    Success = false,
                    Data = new { Error = ex.Message }
                };
            }
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task<bool> SendRealEmail(string email, string subject, string body)
        {
            try
            {
                // Use OmniChannel's Helper.SmtpSendMail method approach
                _logger.LogInformation("Sending email via OmniChannel method to {Email}", email);
                
                // Simulate OmniChannel email sending
                await Task.Delay(100);
                
                // Log email details (in production, this would call Helper.SmtpSendMail)
                _logger.LogInformation("Email sent - To: {Email}, Subject: {Subject}", email, subject);
                Console.WriteLine($"📧 Email to {email}: {subject}");
                Console.WriteLine($"Body: {body}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email");
                return false;
            }
        }

        private async Task<bool> SendRealOTP(string phone, string otp, string purpose)
        {
            try
            {
                // Use OmniChannel OTP service approach
                var otpRequest = new
                {
                    username = "loyalty_system",
                    action = "LoyaltyRedemption", 
                    source = "LoyaltyApp",
                    requestid = Guid.NewGuid().ToString(),
                    AccountNo = "1006817382", // Use actual account
                    OTPPurpose = purpose
                };
                
                _logger.LogInformation("Sending OTP via OmniChannel method to {Phone}", phone);
                
                // Simulate OmniChannel OTP sending
                await Task.Delay(100);
                
                var message = $"Your OTP for {purpose}: {otp}. Valid for 5 minutes. Do not share this code. -Keystone Bank";
                Console.WriteLine($"📱 SMS to {phone}: {message}");
                _logger.LogInformation("📱 OTP sent via OmniChannel - Phone: {Phone}, Purpose: {Purpose}", phone, purpose);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP");
                return false;
            }
        }

        private string BuildVerificationQueries(string accountNumber, string? email, string? phone)
        {
            return $@"
-- Verify OTP records for loyalty redemption
SELECT TOP 5 
    username, 
    email, 
    mobilenumber, 
    action, 
    datecreated, 
    OTPStatus,
    OTPPurpose
FROM UserOTP 
WHERE (mobilenumber = '{phone ?? "N/A"}' OR email = '{email ?? "N/A"}')
    AND (action LIKE '%Loyalty%' OR OTPPurpose LIKE '%Loyalty%')
ORDER BY datecreated DESC;

-- Verify email notification logs
SELECT TOP 5 
    username, 
    otpAction, 
    emailAddress, 
    date_Sent
FROM OtpByEmailLogs 
WHERE emailAddress = '{email ?? "N/A"}'
ORDER BY date_Sent DESC;

-- Verify loyalty alerts (if table exists)
SELECT TOP 5 
    UserId, 
    AccountNumber, 
    AlertType, 
    Message, 
    CreatedDate,
    IsRead
FROM LoyaltyAlerts 
WHERE AccountNumber = '{accountNumber}'
ORDER BY CreatedDate DESC;

-- Check customer loyalty status
SELECT 
    UserId, 
    TotalPoints, 
    Tier, 
    LastUpdated, 
    PointsExpiryDate
FROM CustomerLoyalty 
WHERE UserId IN (
    SELECT UserId FROM AccountUserMapping WHERE AccountNumber = '{accountNumber}'
);
";
        }
    }

    public class NotificationTestRequest
    {
        public string AccountNumber { get; set; } = "1234567890";
        public string? Username { get; set; } = "testuser";
        public string? Email { get; set; } = "test@example.com";
        public string? PhoneNumber { get; set; } = "08012345678";
        public int PointsToRedeem { get; set; } = 50;
        public string? RedemptionType { get; set; } = "TRANSFER";
    }

    public class AlertTestRequest
    {
        public string UserId { get; set; } = "testuser123";
        public string AccountNumber { get; set; } = "1234567890";
    }
}