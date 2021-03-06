using KafkaFlow;
using KafkaFlow.TypedHandler;
using KafkaFlow.Producers;
using Events;

namespace CiamReplicationMonitorMock;

public class CiamReplicationValidator :
    IMessageHandler<CiamProvisionCompletedEvent>
{
    private readonly IMessageProducer _producer;

    public CiamReplicationValidator(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, CiamProvisionCompletedEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(10)); // CIAM takes forever to replicate

        var endTime = DateTime.UtcNow;

        await _producer.ProduceAsync(context.Message.Key, new CiamReplicationVerifiedEvent(message)
        {
            BusinessUnitId = message.BusinessUnitId,
            StartTime = startTime,
            EndTime = endTime
        });
    }
}
