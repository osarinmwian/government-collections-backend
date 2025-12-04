using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace KeyLoyalty.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RealNotificationTestController : ControllerBase
    {
        private readonly ILogger<RealNotificationTestController> _logger;

        public RealNotificationTestController(ILogger<RealNotificationTestController> logger)
        {
            _logger = logger;
        }

        [HttpPost("send-real-email")]
        public async Task<IActionResult> SendRealEmail([FromBody] EmailTestRequest request)
        {
            try
            {
                var subject = request.IsSuccess ? "✅ Loyalty Redemption Success" : "❌ Loyalty Redemption Failed";
                var body = request.IsSuccess 
                    ? $"Your redemption of ₦{request.Amount:N2} was successful!\nAccount: {request.AccountNumber}\nDate: {DateTime.Now}"
                    : $"Your redemption failed.\nAccount: {request.AccountNumber}\nReason: {request.FailureReason}\nDate: {DateTime.Now}";

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential("your-email@gmail.com", "your-app-password") // Configure these
                };

                var message = new MailMessage("noreply@keystone.com", request.Email, subject, body);
                await client.SendMailAsync(message);

                _logger.LogInformation("Real email sent to {Email}", request.Email);

                return Ok(new
                {
                    Success = true,
                    Message = "✅ Real email sent successfully!",
                    EmailSent = new
                    {
                        To = request.Email,
                        Subject = subject,
                        Body = body,
                        SentAt = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send real email");
                return BadRequest(new
                {
                    Success = false,
                    Message = "❌ Failed to send email",
                    Error = ex.Message,
                    Note = "Configure SMTP settings: Gmail App Password, SMTP server, etc."
                });
            }
        }

        [HttpPost("send-real-sms")]
        public async Task<IActionResult> SendRealSMS([FromBody] SMSTestRequest request)
        {
            try
            {
                var message = request.IsSuccess 
                    ? $"Loyalty redemption successful! ₦{request.Amount:N2} credited to {request.AccountNumber}"
                    : $"Loyalty redemption failed for {request.AccountNumber}. Reason: {request.FailureReason}";

                // Example using HTTP SMS gateway - replace with your provider
                using var client = new HttpClient();
                var smsData = new
                {
                    to = request.PhoneNumber,
                    message = message,
                    from = "KEYSTONE"
                };

                // Replace with your SMS provider URL and credentials
                var response = await client.PostAsJsonAsync("https://api.your-sms-provider.com/send", smsData);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Real SMS sent to {Phone}", request.PhoneNumber);
                    return Ok(new
                    {
                        Success = true,
                        Message = "✅ Real SMS sent successfully!",
                        SMSSent = new
                        {
                            To = request.PhoneNumber,
                            Message = message,
                            SentAt = DateTime.Now
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "❌ SMS sending failed",
                        Error = $"HTTP {response.StatusCode}",
                        Note = "Configure your SMS provider API"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send real SMS");
                return BadRequest(new
                {
                    Success = false,
                    Message = "❌ Failed to send SMS",
                    Error = ex.Message,
                    Note = "Configure SMS gateway: Twilio, Nexmo, or your provider"
                });
            }
        }
    }

    public class EmailTestRequest
    {
        public string Email { get; set; } = "test@example.com";
        public string AccountNumber { get; set; } = "1234567890";
        public bool IsSuccess { get; set; } = true;
        public decimal Amount { get; set; } = 50.00m;
        public string? FailureReason { get; set; }
    }

    public class SMSTestRequest
    {
        public string PhoneNumber { get; set; } = "08012345678";
        public string AccountNumber { get; set; } = "1234567890";
        public bool IsSuccess { get; set; } = true;
        public decimal Amount { get; set; } = 50.00m;
        public string? FailureReason { get; set; }
    }
}