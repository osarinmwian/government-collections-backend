using LoyaltyEvent.Services;
using System;

namespace LoyaltyEvent
{
    public class LoyaltyEventService
    {
        private readonly ILoyaltyEventPublisher _eventPublisher;
        private readonly LoyaltyEventHandler _eventHandler;

        public LoyaltyEventService()
        {
            _eventPublisher = new LoyaltyEventPublisher();
            var loyaltyService = new LoyaltyService();
            _eventHandler = new LoyaltyEventHandler(loyaltyService);
        }

        public void Dispose()
        {
            _eventHandler?.Dispose();
        }
    }
}