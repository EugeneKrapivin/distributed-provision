using Events;

using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.TypedHandler;

namespace AttributesConfigServiceMock;

internal class MergeRulesConfigProvisioner
    : IMessageHandler<UcpSchemaCreatedEvent>
{
    private readonly IMessageProducer _producer;

    public MergeRulesConfigProvisioner(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, UcpSchemaCreatedEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(2));

        var endTime = DateTime.UtcNow;

        // TODO: try catch, propage error

        await _producer.ProduceAsync(context.Message.Key, new MergeRuleCreatedEvent
        {
            CallId = message.CallId,
            CorrelationId = message.CorrelationId,
            StartTime = startTime,
            EndTime = endTime,
        });
    }
}

internal class MatchRulesConfigProvisioner
    : IMessageHandler<UcpSchemaCreatedEvent>
{
    private readonly IMessageProducer _producer;

    public MatchRulesConfigProvisioner(IProducerAccessor producerAccessor)
    {
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task Handle(IMessageContext context, UcpSchemaCreatedEvent message)
    {
        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(2));

        var endTime = DateTime.UtcNow;

        // TODO: try catch, propage error

        await _producer.ProduceAsync(context.Message.Key, new MatchRuleCreatedEvent
        {
            CallId = message.CallId,
            CorrelationId = message.CorrelationId,
            StartTime = startTime,
            EndTime = endTime,
        });
    }
}
