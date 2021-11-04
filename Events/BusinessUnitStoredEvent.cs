using System;

namespace Events;

public class BusinessUnitStoredEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }

    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();

    public BusinessUnitStoredEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public BusinessUnitStoredEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
