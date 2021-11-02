﻿using Events;

using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.TypedHandler;

namespace UcpSchemaServiceMock;

public class UcpSchemaProvisioner :
    IMessageHandler<ViewCreatedEvent>
{
    private readonly IMessageProducer _producer;

    public UcpSchemaProvisioner(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, ViewCreatedEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(5)); // because hades sucks and takes lots of time

        // create 2 views and store to DB

        var endTime = DateTime.UtcNow;

        await _producer.ProduceAsync(context.Message.Key, new UcpSchemaCreatedEvent
        {
            CallId = message.CallId,
            CorrelationId = message.CorrelationId,
            BusinessUnitId = message.BusinessUnitId,
            SchemaType = message.ViewName,
            SchemaId = Guid.NewGuid().ToString(),
            StartTime = startTime,
            EndTime = endTime
        });
    }
}
