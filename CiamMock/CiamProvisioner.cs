using KafkaFlow;
using KafkaFlow.TypedHandler;
using KafkaFlow.Producers;
using Microsoft.Extensions.Logging;
using Events;

namespace CiamProvisionerMock;

public class CiamProvisioner :
    IMessageHandler<ProvisionRequestedEvent>
{
    private readonly IMessageProducer _producer;

    public CiamProvisioner(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, ProvisionRequestedEvent message)
    {
        //if (message.Datacenter != Environment.GetEnvironmentVariable("DATACENTER"))
        //{
        //    // do we care?
        //    return;
        //}
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(2)); // CIAM sux, it takes long
        var siteId = new Random().NextInt64(100000, long.MaxValue);
        var apiKey = Guid.NewGuid().ToString();

        var endTime = DateTime.UtcNow;

        // TODO: try catch, propage error

        await _producer.ProduceAsync(context.Message.Key, new CiamProvisionCompletedEvent(message)
        {
            SiteId = siteId,
            BusinessUnitId = apiKey,
            StartTime = startTime,
            EndTime = endTime,
        });
    }
}
