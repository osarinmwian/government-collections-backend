using KeyLoyalty.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Events
{
    public class LoyaltyEventPublisher : ILoyaltyEventPublisher
    {
        private readonly ChannelWriter<LoyaltyTransactionEvent> _writer;
        private readonly ILogger<LoyaltyEventPublisher> _logger;

        public LoyaltyEventPublisher(ChannelWriter<LoyaltyTransactionEvent> writer, ILogger<LoyaltyEventPublisher> logger)
        {
            _writer = writer;
            _logger = logger;
        }

        public async Task PublishTransferEventAsync(LoyaltyTransferEvent transferEvent)
        {
            _logger.LogInformation("Publishing loyalty transfer event: {TransactionId}", transferEvent.TransactionId);
            await _writer.WriteAsync(transferEvent);
        }

        public async Task PublishAirtimeEventAsync(LoyaltyAirtimeEvent airtimeEvent)
        {
            _logger.LogInformation("Publishing loyalty airtime event: {TransactionId}", airtimeEvent.TransactionId);
            await _writer.WriteAsync(airtimeEvent);
        }

        public async Task PublishBillPaymentEventAsync(LoyaltyBillPaymentEvent billPaymentEvent)
        {
            _logger.LogInformation("Publishing loyalty bill payment event: {TransactionId}", billPaymentEvent.TransactionId);
            await _writer.WriteAsync(billPaymentEvent);
        }
    }
}