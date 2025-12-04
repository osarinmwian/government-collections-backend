using KeyLoyalty.Domain.Events;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Events
{
    public interface ILoyaltyEventPublisher
    {
        Task PublishTransferEventAsync(LoyaltyTransferEvent transferEvent);
        Task PublishAirtimeEventAsync(LoyaltyAirtimeEvent airtimeEvent);
        Task PublishBillPaymentEventAsync(LoyaltyBillPaymentEvent billPaymentEvent);
    }
}