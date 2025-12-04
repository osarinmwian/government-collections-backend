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
    public class KeyLoyaltyHandler : BackgroundService
    {
        private readonly ChannelReader<LoyaltyTransactionEvent> _reader;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KeyLoyaltyHandler> _logger;

        public KeyLoyaltyHandler(ChannelReader<LoyaltyTransactionEvent> reader, IServiceProvider serviceProvider, ILogger<KeyLoyaltyHandler> logger)
        {
            _reader = reader;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var eventData in _reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<ICustomerLoyaltyRepository>();
                    await ProcessKeyLoyaltyAsync(eventData, repository);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing loyalty event: {Message}", ex.Message);
                }
            }
        }

        private async Task ProcessKeyLoyaltyAsync(LoyaltyTransactionEvent keyLoyaltyEvent, ICustomerLoyaltyRepository repository)
        {
            var accountNumber = keyLoyaltyEvent switch
            {
                LoyaltyTransferEvent te => te.AccountNumber,
                LoyaltyAirtimeEvent ae => ae.AccountNumber,
                LoyaltyBillPaymentEvent be => be.AccountNumber,
                _ => string.Empty
            };

            if (!IsValidAccountNumber(accountNumber))
            {
                _logger.LogWarning("Invalid account number: {AccountNumber}", accountNumber);
                return;
            }

            var points = keyLoyaltyEvent switch
            {
                LoyaltyTransferEvent => 2, // NIP Transfer = 2 points
                LoyaltyAirtimeEvent => 1,  // Airtime = 1 point
                LoyaltyBillPaymentEvent => 3, // Bill Payment = 3 points
                _ => 0
            };

            var transactionId = keyLoyaltyEvent switch
            {
                LoyaltyTransferEvent te => te.TransactionId,
                LoyaltyAirtimeEvent ae => ae.TransactionId,
                LoyaltyBillPaymentEvent be => be.TransactionId,
                _ => "Unknown"
            };

            _logger.LogInformation("Processing loyalty event - Account: {Account}, Points: {Points}, TxnId: {TxnId}", 
                accountNumber, points, transactionId);

            // Get userId from account mapping service
            using var scope = _serviceProvider.CreateScope();
            var accountMapping = scope.ServiceProvider.GetRequiredService<KeyLoyalty.Infrastructure.Services.IAccountMappingService>();
            var userId = await accountMapping.GetUserIdByAccountNumberAsync(accountNumber);
            
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No userId found for account number: {AccountNumber}", accountNumber);
                return;
            }
            
            var customer = await repository.GetCustomerByUserIdAsync(userId);
            var isNewCustomer = customer == null;
            
            if (isNewCustomer)
            {
                customer = new CustomerLoyalty { UserId = userId };
                _logger.LogInformation("Creating new customer loyalty record for userId: {UserId}", userId);
            }
            
            var oldPoints = customer!.TotalPoints;
            var oldTier = customer.Tier;
            
            customer.TotalPoints += points;
            customer.Tier = CalculateTier(customer.TotalPoints);
            customer.LastUpdated = DateTime.UtcNow;
            
            await repository.UpdateCustomerAsync(customer);
            
            _logger.LogInformation("Loyalty points allocated - Account: {Account}, Points: {OldPoints} -> {NewPoints}, Tier: {OldTier} -> {NewTier}, TxnId: {TxnId}", 
                accountNumber, oldPoints, customer.TotalPoints, oldTier, customer.Tier, transactionId);
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