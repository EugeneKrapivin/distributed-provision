using System;
using System.Threading.Tasks;

using CoordinatorApi.Grains;

using Events;

using KafkaFlow;
using KafkaFlow.Producers;
using KafkaFlow.TypedHandler;

using Microsoft.Extensions.Logging;

using Orleans;

namespace CoordinatorApi.Orchestrators;

public class BusinessUnitProvisioningOrchestrator :
    IMessageHandler<CiamProvisionCompletedEvent>,
    IMessageHandler<CiamReplicationVerifiedEvent>,
    IMessageHandler<ViewCreatedEvent>,
    IMessageHandler<UcpSchemaCreatedEvent>,
    IMessageHandler<MatchRuleCreatedEvent>,
    IMessageHandler<MergeRuleCreatedEvent>
{
    private readonly IClusterClient _client;
    private readonly ILogger<BusinessUnitProvisioningOrchestrator> _logger;
    private readonly IMessageProducer _producer;

    public BusinessUnitProvisioningOrchestrator(IProducerAccessor producerAccessor, IClusterClient client,
        ILogger<BusinessUnitProvisioningOrchestrator> logger)
    {
        _client = client;
        _logger = logger;
        _producer = producerAccessor.GetProducer("producer");
    }

    public async Task<ProvisionMetadata> ProvisionBusinessUnit(string name, string datacenter)
    {

        var correlationId = Guid.NewGuid();

        _logger.LogInformation(
            "Starting provisioning flow with id {correlationId} for business unit named: {name} in datacenter: {datacenter}",
            correlationId,
            name,
            datacenter);

        var ev = new ProvisionRequestedEvent
        {
            CorrelationId = correlationId,
            Name = name,
            Datacenter = datacenter
        };

        // TODO: this probably is not very stable, should protect from failure to push message to kfk
        var response = await _client.GetGrain<IProvisionStatusGrain>(correlationId)
            .Provision(name, null, datacenter);

        await _client.GetGrain<IProvisionStatusGrain>(correlationId)
            .StartStep("ciam provision");

        await _producer.ProduceAsync(correlationId.ToString(), ev);

        return response;
    }

    public async Task Handle(IMessageContext context, CiamProvisionCompletedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
            .EndStep("ciam provision");

        var startTime = DateTime.UtcNow;

        await Task.Delay(TimeSpan.FromSeconds(1)); //TODO BU handling logic in BU grain

        var endTime = DateTime.UtcNow;

        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
                        .LogStep("business unit store", startTime, endTime);

        await _producer.ProduceAsync(context.Message.Key, new BusinessUnitStoredEvent
        {
            BusinessUnitId = message.BusinessUnitId,
            CorrelationId = message.CorrelationId,
            CallId = message.CallId,
            StartTime = startTime,
            EndTime = endTime
        });
    }

    public async Task Handle(IMessageContext context, CiamReplicationVerifiedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
           .LogStep("ciam replication verifivation", message.StartTime, message.EndTime);
    }

    public async Task Handle(IMessageContext context, MergeRuleCreatedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
            .LogStep("create default merge rule", message.StartTime, message.EndTime);
    }

    public async Task Handle(IMessageContext context, MatchRuleCreatedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
            .LogStep("create default match rule", message.StartTime, message.EndTime);
    }

    public async Task Handle(IMessageContext context, UcpSchemaCreatedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
            .LogStep("create default ucp schema", message.StartTime, message.EndTime);
    }

    public async Task Handle(IMessageContext context, ViewCreatedEvent message)
    {
        await _client.GetGrain<IProvisionStatusGrain>(message.CorrelationId)
            .LogStep("create default view", message.StartTime, message.EndTime);
    }
}
