using System;

namespace Events;

public class UcpSchemaCreatedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string SchemaId { get; set; }
    public string SchemaType { get; set; }

    public Guid TriggerMessageId { get; init; }
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public UcpSchemaCreatedEvent()
    {
        TriggerMessageId = Guid.Empty;
    }

    public UcpSchemaCreatedEvent(IBusinessUnitProvisionEvent triggerMessage)
    {
        TriggerMessageId = triggerMessage.MessageId;
        CallId = triggerMessage.CallId;
        CorrelationId = triggerMessage.CorrelationId;
    }
}
