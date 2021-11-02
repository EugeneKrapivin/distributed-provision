
using Orleans;

using System;
using System.Threading.Tasks;

namespace CoordinatorApi.Grains;

public interface IProvisionStatusGrain : IGrainWithGuidKey
{
    Task<ProvisionMetadata> Provision(string name, string description, string datacenter);

    ValueTask<ProvisionMetadata> GetStatus();

    Task<ProvisionStep> StartStep(string name, DateTime? startTime = null);

    Task<ProvisionStep> EndStep(string name, DateTime? endTime = null);

    Task<ProvisionStep> LogStep(string name, DateTime startTime, DateTime endTime);
}
