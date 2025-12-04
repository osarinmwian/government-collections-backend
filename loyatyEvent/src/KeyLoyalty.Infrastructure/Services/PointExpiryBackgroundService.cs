using KeyLoyalty.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeyLoyalty.Infrastructure.Services
{
    public class PointExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PointExpiryBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

        public PointExpiryBackgroundService(IServiceProvider serviceProvider, ILogger<PointExpiryBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Point Expiry Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPointExpiryAsync();
                    await ProcessExpiryRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Point Expiry Background Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Point Expiry Background Service stopped");
        }

        private async Task ProcessPointExpiryAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ICustomerLoyaltyRepository>();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

                _logger.LogInformation("Processing point expiry...");

                var expiredCustomers = await repository.GetCustomersWithExpiredPointsAsync(DateTime.UtcNow);
                var expiredCount = 0;
                var totalExpiredPoints = 0;

                foreach (var customer in expiredCustomers)
                {
                    if (customer.TotalPoints > 0)
                    {
                        totalExpiredPoints += customer.TotalPoints;
                        expiredCount++;
                        
                        _logger.LogInformation("Expiring {Points} points for customer {UserId}", customer.TotalPoints, customer.UserId);
                        
                        // Clear expired points
                        customer.TotalPoints = 0;
                        customer.Tier = Domain.Entities.LoyaltyTier.Bronze;
                        customer.LastUpdated = DateTime.UtcNow;
                        customer.PointsExpiryDate = DateTime.UtcNow.AddYears(1); // Reset expiry
                        
                        await repository.UpdateCustomerAsync(customer);
                        
                        // Send expiry notification
                        await alertService.SendPointExpiryNotificationAsync(customer.UserId, totalExpiredPoints);
                    }
                }

                _logger.LogInformation("Point expiry processing completed. Users affected: {ExpiredCount}, Total expired points: {TotalExpiredPoints}", 
                    expiredCount, totalExpiredPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing point expiry");
            }
        }

        private async Task ProcessExpiryRemindersAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ICustomerLoyaltyRepository>();
                var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

                _logger.LogInformation("Processing expiry reminders...");

                var reminderDays = new[] { 30, 7, 1 };
                var totalReminders = 0;

                foreach (var days in reminderDays)
                {
                    var targetDate = DateTime.UtcNow.AddDays(days).Date;
                    var customers = await repository.GetCustomersWithPointsExpiringOnAsync(targetDate);
                    
                    foreach (var customer in customers)
                    {
                        if (customer.TotalPoints > 0)
                        {
                            await alertService.SendPointExpiryReminderAsync(customer.UserId, customer.TotalPoints, days);
                            totalReminders++;
                            _logger.LogDebug("Sent {Days}-day expiry reminder to customer {UserId} for {Points} points", 
                                days, customer.UserId, customer.TotalPoints);
                        }
                    }
                    
                    _logger.LogInformation("Sent {Count} reminders for {Days}-day expiry", customers.Count, days);
                }

                _logger.LogInformation("Expiry reminders processing completed. Total reminders sent: {TotalReminders}", totalReminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expiry reminders");
            }
        }
    }
}