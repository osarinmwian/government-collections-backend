using LoyaltyEvent.Events;
using LoyaltyEvent.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LoyaltyEvent.Handlers
{
    public class LoyaltyEventHandler
    {
        private readonly ILoyaltyService _loyaltyService;
        private readonly Timer _timer;

        public LoyaltyEventHandler(ILoyaltyService loyaltyService)
        {
            _loyaltyService = loyaltyService;
            _timer = new Timer(ProcessEvents, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private async void ProcessEvents(object state)
        {
            while (LoyaltyEventPublisher.EventQueue.TryDequeue(out BaseEvent eventData))
            {
                try
                {
                    await HandleEvent(eventData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing loyalty event: {ex.Message}");
                }
            }
        }

        private async Task HandleEvent(BaseEvent eventData)
        {
            switch (eventData)
            {
                case TransactionLoyaltyEvent transactionEvent:
                    await _loyaltyService.ProcessTransactionLoyalty(transactionEvent);
                    break;
                case AirtimeLoyaltyEvent airtimeEvent:
                    await _loyaltyService.ProcessAirtimeLoyalty(airtimeEvent);
                    break;
                case BillPaymentLoyaltyEvent billEvent:
                    await _loyaltyService.ProcessBillPaymentLoyalty(billEvent);
                    break;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}