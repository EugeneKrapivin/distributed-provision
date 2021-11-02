using KafkaFlow;
using KafkaFlow.TypedHandler;
using KafkaFlow.Producers;
using Events;

namespace CiamReplicationMonitorMock;

public class CiamReplicationValidator :
    IMessageHandler<CiamProvisionCompletedEvent>
{
    private readonly IProducerAccessor _producerAccessor;

    public CiamReplicationValidator(IProducerAccessor producerAccessor)
    {
        _producerAccessor = producerAccessor;
    }

    public async Task Handle(IMessageContext context, CiamProvisionCompletedEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(10)); // CIAM takes forever to replicate

        var endTime = DateTime.UtcNow;

        await _producerAccessor.GetProducer("producer").ProduceAsync(context.Message.Key, new CiamReplicationVerifiedEvent
        {
            CallId = message.CallId,
            CorrelationId = message.CorrelationId,
            BusinessUnitId = message.BusinessUnitId,
            StartTime = startTime,
            EndTime = endTime
        });
    }
}
