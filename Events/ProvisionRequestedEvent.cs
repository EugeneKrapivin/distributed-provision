using System;

namespace Events;

public class ProvisionRequestedEvent : IBusinessUnitProvisionEvent
{
    public string CallId { get; init; }
    public Guid CorrelationId { get; init; }
    public string BusinessUnitId { get; init; }

    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Name { get; set; }
    public string Datacenter { get; set; }
}
