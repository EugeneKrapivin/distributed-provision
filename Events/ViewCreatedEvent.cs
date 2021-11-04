using System;

namespace Events;

public class ViewCreatedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string ViewId { get; set; }
    public string ViewName { get; set; }

    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public ViewCreatedEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public ViewCreatedEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
