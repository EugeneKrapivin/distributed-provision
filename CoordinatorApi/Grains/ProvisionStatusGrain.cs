using AutoMapper;

using Events;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoordinatorApi.Grains;

public enum StepType
{
    CiamProvisioned,
    CiamReplicationVerified,
    BusinessUnitStored,
    ViewCreated,
    CustomerSchemaCreated,
    MatchRuleCreated,
    MergeRuleCreated,
}

public enum StepStatus
{
    InProgress,
    Finished,
    Failed
}

public enum ProvisionStatus
{
    InProgress,
    Finished,
    Failed
}

public class ProvisionStep
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepType Type { get; set; }
    
    public string Name { get; set; }
    
    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StepStatus Status { get; set; }
    
    public Dictionary<string, string> Metadata { get; set; } = new();
}

// use to return data from grain
public class ProvisionMetadata
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Datacenter { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisionStatus Status { get; init; }

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
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProvisionStatus Status { get; set; }

    public Dictionary<string, ProvisionStep> Steps { get; set; } = new();

    public DateTime Created { get; init; }
    public DateTime Updated { get; set; }
}

public class ProvisionStatusGrain : Grain, IProvisionStatusGrain
{
    private readonly IPersistentState<ProvisionState> _provisionState;
    private readonly ILogger<ProvisionStatusGrain> _logger;
    private static readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.CreateMap<ProvisionState, ProvisionMetadata>())
        .CreateMapper();

    public ProvisionStatusGrain(
        [PersistentState("provision-state", "provision-store")] IPersistentState<ProvisionState> provisionState,
        ILogger<ProvisionStatusGrain> logger)
    {
        _provisionState = provisionState;
        this._logger = logger;
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
            Status = ProvisionStatus.InProgress,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
        };

        await _provisionState.WriteStateAsync();

        return _mapper.Map<ProvisionMetadata>(_provisionState.State);
    }

    public async Task<ProvisionStep> StartStep(string messageId, StepType type, DateTime? startTime = null)
    {
        if (_provisionState.State.Status == ProvisionStatus.Finished)
        {
            throw new Exception("got a new step logging in a completed provision flow");
        }

        if (!_provisionState.State.Steps.ContainsKey(messageId))
        {
            _provisionState.State.Steps[messageId] = new ProvisionStep
            {
                Name = messageId,
                Type = type,
                StartedAt = startTime ?? DateTime.UtcNow,
                Status = StepStatus.InProgress
            };
        }
        else
        {
            throw new Exception($"Cannot start already started step");
        }

        await Update();

        return _provisionState.State.Steps[messageId];
    }

    public async Task<ProvisionStep> EndStep(string messageId, DateTime? endTime = null)
    {
        if (_provisionState.State.Status == ProvisionStatus.Finished)
        {
            throw new Exception("got a new step logging in a completed provision flow");
        }

        if (_provisionState.State.Steps.ContainsKey(messageId))
        {
            _provisionState.State.Steps[messageId].CompletedAt = endTime ?? DateTime.UtcNow;
            _provisionState.State.Steps[messageId].Status = StepStatus.Finished;
        }
        else
        {
            throw new Exception($"Cannot finish unstarted step");
        }

        await CheckCompleted();
        await Update();

        return _provisionState.State.Steps[messageId];
    }

    private async Task Update()
    {
        _provisionState.State.Updated = DateTime.UtcNow;
        await _provisionState.WriteStateAsync();
    }

    private async Task CheckCompleted()
    {
        var state = _provisionState.State;
        if (state.Status == ProvisionStatus.Finished || state.Status == ProvisionStatus.Failed) // state should be able to change to completed only once
        {
            return;
        }

        var checkList = GetCheckList();

        var executedSteps = _provisionState.State.Steps.Values.GroupBy(x => x.Type)
            .Select(x => (type: x.Key, count: x.Count()));

        foreach (var executedStep in executedSteps)
        {
            checkList[executedStep.type] = checkList[executedStep.type] - executedStep.count;
        }

        if (checkList.Values.All(x => x == 0))
        {
            state.Status = ProvisionStatus.Finished;
            await Update();
        }

        static Dictionary<StepType, int> GetCheckList() => new()
        {
            [StepType.CiamProvisioned] = 1,
            [StepType.BusinessUnitStored] = 1,
            [StepType.CiamReplicationVerified] = 1,
            [StepType.ViewCreated] = 2,
            [StepType.CustomerSchemaCreated] = 2,
            [StepType.MergeRuleCreated] = 4,
            [StepType.MatchRuleCreated] = 4,

        };
    }

    public async Task<ProvisionStep> LogStep<T>(T businessUnitEvent) where T : IBusinessUnitProvisionEvent
    {
        if (_provisionState.State.Status == ProvisionStatus.Finished)
        {
            _logger.LogWarning("got a new step logging in a completed provision flow", businessUnitEvent);
            throw new Exception("got a new step logging in a completed provision flow");
        }
        var step = HandleEvent(businessUnitEvent);

        _provisionState.State.Steps.Add(businessUnitEvent.MessageId.ToString(), step);

        await CheckCompleted();
        
        return step;
    }

    private static ProvisionStep HandleEvent<T>(T businessUnitEvent) where T : IBusinessUnitProvisionEvent
    {
        var step = CreateStep(businessUnitEvent);

        // could be better handled by inversion of controll. Pass Step into Event
        switch(businessUnitEvent)
        {
            case BusinessUnitStoredEvent e:
                step.Name = "business unit stored";
                step.Type = StepType.BusinessUnitStored;
                step.Metadata["businessUnitId"] = e.BusinessUnitId;
                break;
            case CiamProvisionCompletedEvent e:
                step.Name = "ciam privioning completed";
                step.Type = StepType.CiamProvisioned;
                step.Metadata["siteId"] = e.SiteId.ToString();
                step.Metadata["businessUnitId"] = e.BusinessUnitId;
                break;
            case CiamReplicationVerifiedEvent:
                step.Name = "ciam privioning verified";
                step.Type = StepType.CiamReplicationVerified;
                break;
            case ViewCreatedEvent e:
                step.Name = $"view \"{e.ViewName}\" ({e.ViewId}) created";
                step.Type = StepType.ViewCreated;
                step.Metadata["viewName"] = e.ViewName;
                step.Metadata["viewId"] = e.ViewId;
                break;
            case UcpSchemaCreatedEvent e:
                step.Name = $"customer \"{e.SchemaType}\" schema ({e.SchemaId}) created";
                step.Type = StepType.CustomerSchemaCreated;
                step.Metadata["schemaId"] = e.SchemaId;
                step.Metadata["schemaType"] = e.SchemaType;
                break;
            case MergeRuleCreatedEvent e:
                step.Name = $"merge rule for \"{e.Name}\"({e.RuleId}) created";
                step.Type = StepType.MergeRuleCreated;
                step.Metadata["schemaId"] = e.SchemaId;
                step.Metadata["ruleName"] = e.Name;
                step.Metadata["ruleId"] = e.RuleId;
                break;
            case MatchRuleCreatedEvent e:
                step.Name = $"match rule for \"{e.Name}\"({e.RuleId}) created";
                step.Type = StepType.MatchRuleCreated;
                step.Metadata["schemaId"] = e.SchemaId;
                step.Metadata["ruleName"] = e.Name;
                step.Metadata["ruleId"] = e.RuleId;
                break;

        }

        return step;

        static ProvisionStep CreateStep(IBusinessUnitProvisionEvent @event) =>
            new()
            {
                StartedAt = @event.StartTime,
                Status = StepStatus.Finished,
                CompletedAt = @event.EndTime
            };
    }
}

