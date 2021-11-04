using Events;

using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.TypedHandler;

namespace AttributesConfigServiceMock;

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

        // probably could be better handled with a mediator :)
        if (message.SchemaType == "marketing")
        {
            foreach (var mergeRule in new[] { "email", "phone" })
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var endTime = DateTime.UtcNow;
                await PublishEvent(context, message, startTime, endTime, mergeRule);
            }
        }
        else if (message.SchemaType == "operational")
        {
            foreach (var mergeRule in new[] { "ciamId", "crmId" })
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var endTime = DateTime.UtcNow;
                await PublishEvent(context, message, startTime, endTime, mergeRule);
            }
        }
    }

    private async Task PublishEvent(IMessageContext context, UcpSchemaCreatedEvent message, DateTime startTime, DateTime endTime, string mergeRule)
    => await _producer.ProduceAsync(context.Message.Key, new MatchRuleCreatedEvent(message)
    {
        StartTime = startTime,
        EndTime = endTime,
        Name = mergeRule,
        RuleId = Guid.NewGuid().ToString(),
        SchemaId = message.SchemaId
    });
}
