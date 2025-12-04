using KeyLoyalty.Domain.Events;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KeyLoyalty.Infrastructure.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T eventData) where T : BaseEvent;
    }

    public class EventPublisher : IEventPublisher
    {
        private readonly ChannelWriter<BaseEvent> _writer;

        public EventPublisher(ChannelWriter<BaseEvent> writer)
        {
            _writer = writer;
        }

        public async Task PublishAsync<T>(T eventData) where T : BaseEvent
        {
            await _writer.WriteAsync(eventData);
        }
    }
}