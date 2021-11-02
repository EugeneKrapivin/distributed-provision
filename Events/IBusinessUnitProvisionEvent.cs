using System;

namespace Events;

public interface IBusinessUnitProvisionEvent
{
    public Guid MessageId => Guid.NewGuid();
    public DateTime CreatedAt => DateTime.UtcNow;

    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}
