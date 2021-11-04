
using Events;

using Orleans;

using System;
using System.Threading.Tasks;

namespace CoordinatorApi.Grains;

public interface IProvisionStatusGrain : IGrainWithGuidKey
{
    Task<ProvisionMetadata> Provision(string name, string description, string datacenter);

    ValueTask<ProvisionMetadata> GetStatus();

    Task<ProvisionStep> StartStep(string messageId, StepType type, DateTime? startTime = null);

    Task<ProvisionStep> EndStep(string messageId, DateTime? endTime = null);

    Task<ProvisionStep> LogStep<T>(T step) 
        where T : IBusinessUnitProvisionEvent;
}
