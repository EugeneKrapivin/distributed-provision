using System;

namespace Events;

public class MergeRuleCreatedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Name { get; set; }
    public string RuleId { get; set; }
    public string SchemaId { get; set; }

    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public MergeRuleCreatedEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public MergeRuleCreatedEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
