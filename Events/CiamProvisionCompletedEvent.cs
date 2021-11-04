using System;

namespace Events;

public class CiamProvisionCompletedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }
    
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public long SiteId { get; set; }
    
    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();

    public CiamProvisionCompletedEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public CiamProvisionCompletedEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
