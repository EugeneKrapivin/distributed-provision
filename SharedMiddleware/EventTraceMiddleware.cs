using Events;

using KafkaFlow;

using Microsoft.Extensions.Logging;

namespace SharedMiddleware;

public class EventTraceMiddleware : IMessageMiddleware
{
    private readonly ILogger<EventTraceMiddleware> _logger;

    public EventTraceMiddleware(ILogger<EventTraceMiddleware> logger)
    {
        _logger = logger;
    }

    public Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        _logger.LogInformation(
                "Handling ({event}) event for flow with id:{correlationId}",
                context.Message.Value.GetType().Name,
                (context.Message.Value as IBusinessUnitProvisionEvent)?.CorrelationId.ToString() ?? "null");

        return next(context);
    }
}
