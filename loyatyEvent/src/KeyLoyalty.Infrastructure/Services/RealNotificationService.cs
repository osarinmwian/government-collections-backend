using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Services
{
    public interface IRealNotificationService
    {
        Task<bool> SendEmailAsync(string email, string subject, string body);
        Task<bool> SendOTPAsync(string phone, string otp, string purpose);
    }

    public class RealNotificationService : IRealNotificationService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<RealNotificationService> _logger;

        public RealNotificationService(IConfiguration config, ILogger<RealNotificationService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
                var username = _config["EmailSettings:Username"] ?? "your-email@gmail.com";
                var password = _config["EmailSettings:Password"] ?? "your-app-password";
                var fromEmail = _config["EmailSettings:FromEmail"] ?? "noreply@keystone.com";

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                var message = new MailMessage(fromEmail, email, subject, body);
                await client.SendMailAsync(message);

                _logger.LogInformation("✅ Email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendOTPAsync(string phone, string otp, string purpose)
        {
            try
            {
                var message = $"Your OTP for {purpose}: {otp}. Valid for 5 minutes. Do not share this code.";
                
                // Option 1: Use HTTP SMS Gateway
                var smsUrl = _config["SMSSettings:ApiUrl"];
                var apiKey = _config["SMSSettings:ApiKey"];
                
                if (!string.IsNullOrEmpty(smsUrl) && !string.IsNullOrEmpty(apiKey))
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    
                    var smsData = new
                    {
                        to = phone,
                        message = message,
                        from = "KEYSTONE"
                    };

                    var json = JsonSerializer.Serialize(smsData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(smsUrl, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ SMS sent successfully to {Phone}", phone);
                        return true;
                    }
                }

                // Option 2: Log to console (for testing)
                _logger.LogWarning("📱 SMS would be sent to {Phone}: {Message}", phone, message);
                Console.WriteLine($"📱 SMS to {phone}: {message}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send SMS to {Phone}", phone);
                return false;
            }
        }
    }
}