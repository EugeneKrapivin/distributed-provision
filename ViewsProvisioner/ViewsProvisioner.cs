using Events;

using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.TypedHandler;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewsProvisionerMock;

public class ViewsProvisioner :
    IMessageHandler<BusinessUnitStoredEvent>
{
    private static readonly string[] _defaultViews = new[] { "marketing", "operational" };
    private readonly IMessageProducer _producer;

    public ViewsProvisioner(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, BusinessUnitStoredEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(1));

        // create 2 views and store to DB

        var endTime = DateTime.UtcNow;

        foreach (var view in _defaultViews)
        {
            await _producer.ProduceAsync(context.Message.Key, new ViewCreatedEvent
            {
                CallId = message.CallId,
                CorrelationId = message.CorrelationId,
                BusinessUnitId = message.BusinessUnitId,
                ViewName = view,
                ViewId = Guid.NewGuid().ToString(),
                StartTime = startTime,
                EndTime = endTime
            });
        }
    }
}
