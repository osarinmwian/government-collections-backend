using KeyLoyalty.Domain.Entities;
using KeyLoyalty.Domain.Events;
using KeyLoyalty.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Events
{
    public class LoyaltyEventListener : BackgroundService
    {
        private readonly ChannelReader<LoyaltyTransactionEvent> _reader;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoyaltyEventListener> _logger;

        public LoyaltyEventListener(
            ChannelReader<LoyaltyTransactionEvent> reader, 
            IServiceProvider serviceProvider,
            ILogger<LoyaltyEventListener> logger)
        {
            _reader = reader;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Loyalty Event Listener started");
            
            await foreach (var loyaltyEvent in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<ICustomerLoyaltyRepository>();
                    await ProcessLoyaltyEventAsync(loyaltyEvent, repository);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing loyalty event: {EventType}", loyaltyEvent.GetType().Name);
                }
            }
        }

        private async Task ProcessLoyaltyEventAsync(LoyaltyTransactionEvent loyaltyEvent, ICustomerLoyaltyRepository repository)
        {
            var accountNumber = loyaltyEvent.AccountNumber;
            
            if (!IsValidAccountNumber(accountNumber))
            {
                _logger.LogWarning("Invalid account number: {AccountNumber}", accountNumber);
                return;
            }

            var points = CalculatePoints(loyaltyEvent);
            
            if (points == 0)
            {
                _logger.LogDebug("No points awarded for transaction {TransactionId}", loyaltyEvent.TransactionId);
                return;
            }

            _logger.LogInformation("Processing loyalty event: {EventType} for account {AccountNumber}, awarding {Points} points", 
                loyaltyEvent.GetType().Name, accountNumber, points);

            // Get userId from account mapping service
            using var scope = _serviceProvider.CreateScope();
            var accountMapping = scope.ServiceProvider.GetRequiredService<KeyLoyalty.Infrastructure.Services.IAccountMappingService>();
            var userId = await accountMapping.GetUserIdByAccountNumberAsync(accountNumber);
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No userId found for account number: {AccountNumber}", accountNumber);
                return;
            }
            
            var customer = await repository.GetCustomerByUserIdAsync(userId) ?? 
                          new CustomerLoyalty { UserId = userId };
            
            customer.TotalPoints += points;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            await repository.UpdateCustomerAsync(customer);
            
            _logger.LogInformation("Updated customer {AccountNumber}: {TotalPoints} points, {Tier} tier", 
                accountNumber, customer.TotalPoints, customer.Tier);
        }

        private int CalculatePoints(LoyaltyTransactionEvent loyaltyEvent)
        {
            return loyaltyEvent switch
            {
                LoyaltyTransferEvent transfer => transfer.Amount >= 1000 ? 2 : 0,
                LoyaltyAirtimeEvent => 1,
                LoyaltyBillPaymentEvent => 3,
                _ => 0
            };
        }

        private static bool IsValidAccountNumber(string? accountNumber)
        {
            return !string.IsNullOrWhiteSpace(accountNumber) && 
                   accountNumber.Length == 10 && 
                   accountNumber.All(char.IsDigit);
        }

        private LoyaltyTier CalculateTier(int points)
        {
            return points switch
            {
                >= 10001 => LoyaltyTier.Diamond,
                >= 6001 => LoyaltyTier.Platinum,
                >= 3001 => LoyaltyTier.Gold,
                >= 501 => LoyaltyTier.Silver,
                _ => LoyaltyTier.Bronze
            };
        }
    }
}