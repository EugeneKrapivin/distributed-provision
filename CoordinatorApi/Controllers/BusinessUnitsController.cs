using CoordinatorApi.Grains;
using CoordinatorApi.Orchestrators;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Orleans;

using System;
using System.Threading.Tasks;

namespace CoordinatorApi.Controllers;

[ApiController]
[Produces("application/json")]
[Route("[controller]")]
public class BusinessUnitsController : ControllerBase
{
    private readonly BusinessUnitProvisioningOrchestrator _provisioner;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<BusinessUnitsController> _logger;

    public BusinessUnitsController(BusinessUnitProvisioningOrchestrator provisioner, IClusterClient clusterClient, ILogger<BusinessUnitsController> logger)
    {
        _provisioner = provisioner;
        _clusterClient = clusterClient;
        _logger = logger;
    }

    [HttpGet("{id}", Name = "get-provision")]
    public async Task<ProvisionMetadata> Get(Guid id)
    {
        return await _clusterClient.GetGrain<IProvisionStatusGrain>(id)
            .GetStatus();
    }

    /// <summary>
    /// Kick of a provision request.
    /// </summary>
    /// <param name="request">The request json from the client</param>
    /// <returns>Provisioning request status</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(ProvisioningStatus))]
    public async Task<ActionResult<ProvisioningStatus>> Create([FromBody] ProvisionBusinessUnitRequest request)
    {
        var provisionResult = await _provisioner.ProvisionBusinessUnit(request.Name, request.Datacenter);

        return AcceptedAtRoute("get-provision",
            new { id = provisionResult.Id.ToString() },
            new ProvisioningStatus
            {
                Id = provisionResult.Id.ToString(),
                Status = provisionResult.Status,
                CreatedAt = provisionResult.Created,
                LastUpdate = provisionResult.Updated
            });
    }
}

public class ProvisioningStatus
{
    public string Id { get; init; }
    public string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdate { get; init; }
}
