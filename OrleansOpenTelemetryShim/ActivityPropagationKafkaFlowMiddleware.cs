using KafkaFlow;

using System.Diagnostics;

namespace OrleansOpenTelemetryShim
{
    using System.Text;

    // if put before typed handle it will for sure trace all messages, hence the tracing should occur inside the typed handler
    public class ActivityPropagationConsumerMiddleware : IMessageMiddleware
    {
        protected const string TraceParentHeaderName = "traceparent";
        protected const string TraceStateHeaderName = "tracestate";

        protected const string ActivitySourceName = "kafkaflow";

        protected static readonly ActivitySource activitySource = new(ActivitySourceName);

        public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
        {
            var traceParent = context.Headers[TraceParentHeaderName] != null 
                ? Encoding.UTF8.GetString(context.Headers[TraceParentHeaderName])
                : null;

            var traceState = context.Headers[TraceStateHeaderName] != null ?
                Encoding.UTF8.GetString(context.Headers[TraceStateHeaderName])
                : null;
            
            var parentContext = new ActivityContext();

            if (traceParent is not null)
            {
                parentContext = ActivityContext.Parse(traceParent, traceState);
            }

            ActivityTagsCollection? tags = default;

            if (activitySource.HasListeners())
            {
                // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md
                tags = new ActivityTagsCollection
                {
                    ["messaging.system"] = "kafka",
                    ["messaging.operation"] = "receive",
                    ["messaging.destination"] = context.ConsumerContext.Topic,
                    ["messaging.destination_kind"] = "topic",
                    ["messaging.temp_destination"] = false,
                    ["messaging.kafka.message_key"] = Encoding.UTF8.GetString(context.Message.Key as Byte[]),

                    // client properties
                    ["messaging.client_id"] = context.ConsumerContext.ConsumerName,
                    ["messaging.message_id"] = context.ConsumerContext.Offset,
                    ["messaging.kafka.partition"] = context.ConsumerContext.Partition,
                    ["messaging.kafka.consumer_group"] = context.ConsumerContext.GroupId
                };
            }

            Activity? activity = null;
            try
            {
                activity = activitySource.StartActivity("kafka.consume", ActivityKind.Consumer, parentContext, tags);

                await next(context);
            }
            catch (Exception e)
            {
                if (activity is { IsAllDataRequested: true })
                {
                    // exception attributes from https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
                    activity.SetTag("exception.type", e.GetType());
                    activity.SetTag("exception.message", e.Message);
                    activity.SetTag("exception.stacktrace", e.StackTrace);
                    activity.SetTag("exception.escaped", true);
                }

                throw;
            }
            finally
            {
                // Activity complete
                activity?.Dispose();
            }
        }
    }
    
    public class ActivityPropagationProcessMiddleware : IMessageMiddleware
    {
        protected const string TraceParentHeaderName = "traceparent";
        protected const string TraceStateHeaderName = "tracestate";

        protected const string ActivitySourceName = "kafkaflow";

        protected static readonly ActivitySource activitySource = new(ActivitySourceName);

        public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
        {
            var traceParent = context.Headers[TraceParentHeaderName] != null
                ? Encoding.UTF8.GetString(context.Headers[TraceParentHeaderName])
                : null;

            var traceState = context.Headers[TraceStateHeaderName] != null ?
                Encoding.UTF8.GetString(context.Headers[TraceStateHeaderName])
                : null;

            var parentContext = new ActivityContext();

            if (traceParent is not null)
            {
                parentContext = ActivityContext.Parse(traceParent, traceState);
            }

            ActivityTagsCollection? tags = default;

            if (activitySource.HasListeners())
            {
                // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md
                tags = new ActivityTagsCollection
                {
                    ["messaging.system"] = "kafka",
                    ["messaging.operation"] = "process",
                    ["messaging.destination"] = context.ConsumerContext.Topic,
                    ["messaging.destination_kind"] = "topic",
                    ["messaging.temp_destination"] = false,
                    ["messaging.kafka.message_key"] = Encoding.UTF8.GetString(context.Message.Key as Byte[]),

                    // client properties
                    ["messaging.client_id"] = context.ConsumerContext.ConsumerName,
                    ["messaging.message_id"] = context.ConsumerContext.Offset,
                    ["messaging.kafka.partition"] = context.ConsumerContext.Partition,
                    ["messaging.kafka.consumer_group"] = context.ConsumerContext.GroupId
                };
            }

            Activity? activity = null;
            try
            {
                activity = activitySource.StartActivity("kafka.process", ActivityKind.Server, parentContext, tags);

                await next(context);
            }
            catch (Exception e)
            {
                if (activity is { IsAllDataRequested: true })
                {
                    // exception attributes from https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
                    activity.SetTag("exception.type", e.GetType());
                    activity.SetTag("exception.message", e.Message);
                    activity.SetTag("exception.stacktrace", e.StackTrace);
                    activity.SetTag("exception.escaped", true);
                }

                throw;
            }
            finally
            {
                // Activity complete
                activity?.Dispose();
            }
        }
    }
    public class ActivityPropagationProducerMiddleware : IMessageMiddleware
    {
        protected const string TraceParentHeaderName = "traceparent";
        protected const string TraceStateHeaderName = "tracestate";

        protected const string ActivitySourceName = "kafkaflow";

        protected static readonly ActivitySource activitySource = new(ActivitySourceName);

        public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
        {
            var currentActivity = Activity.Current;

            if (currentActivity is not null &&
                currentActivity.IdFormat == ActivityIdFormat.W3C)
            {
                context.Headers.Add(TraceParentHeaderName, Encoding.UTF8.GetBytes(currentActivity.Id));
                if (currentActivity.TraceStateString is not null)
                {
                    context.Headers.Add(TraceStateHeaderName, Encoding.UTF8.GetBytes(currentActivity.TraceStateString));
                }
            }

            // Read trace context from RequestContext
            var traceParent = Encoding.UTF8.GetString(context.Headers[TraceParentHeaderName]);
            var traceState = context.Headers[TraceStateHeaderName] != null
                ? Encoding.UTF8.GetString(context.Headers[TraceStateHeaderName])
                : null;
            var parentContext = new ActivityContext();

            if (traceParent is not null)
            {
                parentContext = ActivityContext.Parse(traceParent, traceState);
            }

            ActivityTagsCollection? tags = default;

            if (activitySource.HasListeners())
            {
                // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md
                tags = new ActivityTagsCollection
                {
                    ["messaging.operation"] = "send",
                    ["messaging.system"] = "kafka",
                    ["messaging.destination"] = context.ProducerContext.Topic,
                    ["messaging.destination_kind"] = "topic",
                    ["messaging.temp_destination"] = false,
                    ["messaging.kafka.message_key"] = context.Message.Key as string
                };
            }

            Activity? activity = null;
            try 
            {
                activity = activitySource.StartActivity("kafka.publish", ActivityKind.Producer, parentContext, tags);
                 
                await next(context);
            }
            catch (Exception e)
            {
                if (activity is { IsAllDataRequested: true })
                {
                    // exception attributes from https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/exceptions.md
                    activity.SetTag("exception.type", e.GetType());
                    activity.SetTag("exception.message", e.Message);
                    activity.SetTag("exception.stacktrace", e.StackTrace);
                    activity.SetTag("exception.escaped", true);
                }

                throw;
            }
            finally
            {
                // Activity complete
                activity?.Dispose();
            }
        }
    }
}