using AutoMapper;

using Orleans;
using Orleans.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoordinatorApi.Grains;

public class ProvisionStep
{
    public string Name { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; }
}

// use to return data from grain
public class ProvisionMetadata
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Datacenter { get; init; }

    public string Status { get; init; }

    public Dictionary<string, ProvisionStep> Steps { get; set; } = new();

    public DateTime Created { get; init; }
    public DateTime Updated { get; init; }

}

// use strictly for storage
public class ProvisionState
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Datacenter { get; init; }

    public string Status { get; set; }

    public Dictionary<string, ProvisionStep> Steps { get; set; } = new();

    public DateTime Created { get; init; }
    public DateTime Updated { get; set; }
}

public class ProvisionStatusGrain : Grain, IProvisionStatusGrain
{
    private readonly IPersistentState<ProvisionState> _provisionState;

    private static readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.CreateMap<ProvisionState, ProvisionMetadata>())
        .CreateMapper();

    public ProvisionStatusGrain(
        [PersistentState("provision-state", "provision-store")] IPersistentState<ProvisionState> provisionState)
    {
        _provisionState = provisionState;
    }

    public ValueTask<ProvisionMetadata> GetStatus()
    {
        return ValueTask.FromResult(_mapper.Map<ProvisionMetadata>(_provisionState.State));
    }

    public async Task<ProvisionMetadata> Provision(string name, string description, string datacenter)
    {
        _provisionState.State = new ProvisionState
        {
            Id = this.GetPrimaryKey(),
            Name = name,
            Description = description,
            Datacenter = datacenter,
            Status = "in-progress",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
        };

        await _provisionState.WriteStateAsync();

        return _mapper.Map<ProvisionMetadata>(_provisionState.State);
    }

    public async Task<ProvisionStep> StartStep(string name, DateTime? startTime = null)
    {
        if (_provisionState.State.Status == "done")
        {
            throw new Exception("got a new step logging in a completed provision flow");
        }

        if (_provisionState.State.Steps.ContainsKey(name))
        {
            _provisionState.State.Steps[name].StartedAt = startTime ?? DateTime.UtcNow;
            _provisionState.State.Steps[name].Status = "executing";
        }
        else
        {
            _provisionState.State.Steps[name] = new ProvisionStep
            {
                Name = name,
                StartedAt = startTime ?? DateTime.UtcNow,
                Status = "executing"
            };
        }

        await CheckCompleted();
        await Update();

        return _provisionState.State.Steps[name];
    }

    public async Task<ProvisionStep> EndStep(string name, DateTime? endTime = null)
    {
        if (_provisionState.State.Status == "done")
        {
            throw new Exception("got a new step logging in a completed provision flow");
        }

        if (_provisionState.State.Steps.ContainsKey(name))
        {
            _provisionState.State.Steps[name].CompletedAt = endTime ?? DateTime.UtcNow;
            _provisionState.State.Steps[name].Status = "done";
        }
        else
        {
            _provisionState.State.Steps[name] = new ProvisionStep
            {
                Name = name,
                StartedAt = endTime ?? DateTime.UtcNow,
                CompletedAt = endTime ?? DateTime.UtcNow,
                Status = "done"
            };
        }

        await CheckCompleted();
        await Update();

        return _provisionState.State.Steps[name];
    }

    public async Task<ProvisionStep> LogStep(string name, DateTime startTime, DateTime endTime)
    {
        if (_provisionState.State.Status == "done")
        {
            throw new Exception("got a new step logging in a completed provision flow");
        }

        _provisionState.State.Steps[name] = new ProvisionStep
        {
            Name = name,
            StartedAt = startTime,
            CompletedAt = endTime,
            Status = "done"
        };

        await CheckCompleted();

        return _provisionState.State.Steps[name];
    }

    private async Task Update()
    {
        _provisionState.State.Updated = DateTime.UtcNow;
        await _provisionState.WriteStateAsync();
    }

    private static readonly Dictionary<string, int> requiredSteps = new()
    {
        ["ciam provision"] = 1,
        ["business unit store"] = 1,
        ["ciam replication verifivation"] = 1,
        ["create default view"] = 1,
        ["create default merge rule"] = 1,
        ["create default match rule"] = 1,
        ["create default ucp schema"] = 1,
    };

    private async Task CheckCompleted()
    {
        var state = _provisionState.State;
        if (state.Status == "done") // state should be able to change to completed only once
        {
            return;
        }

        state.Status = "done";

        foreach (var step in requiredSteps)
        {
            if (state.Steps.TryGetValue(step.Key, out var stepLog))
            {
                if (stepLog.Status != "done")
                {
                    state.Status = "in-progress";
                }
            }
            else
            {
                state.Status = "in-progress";
            }
        }

        await Update();
    }
}
