using System;

namespace Events;

public class CiamReplicationVerifiedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }

    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();

    public CiamReplicationVerifiedEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public CiamReplicationVerifiedEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
