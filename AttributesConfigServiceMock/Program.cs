using Microsoft.Extensions.Hosting;
using KafkaFlow;
using KafkaFlow.TypedHandler;
using AttributesConfigServiceMock;
using KafkaFlow.Serializer;

using OrleansOpenTelemetryShim;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

const string groupId = "attributes-config-service";

const string topic = "business-unit-provisioning";

string[] brokers = new[] { "broker:9092" };

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddKafkaFlowHostedService(kafka => kafka
            .UseConsoleLog()
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .AddConsumer(consumer => consumer
                    .Topic(topic)
                    .WithGroupId(groupId)
                    .WithBufferSize(100)
                    .WithWorkersCount(10)
                    .AddMiddlewares(middlewares => middlewares
                        .AddSerializer<NewtonsoftJsonSerializer>()
                        .Add<ActivityPropagationConsumerMiddleware>()
                        .AddTypedHandlers(handlers => handlers
                            .AddHandler<MergeRulesConfigProvisioner>()
                            .AddHandler<MatchRulesConfigProvisioner>()
                        )
                    )
                ).AddProducer("producer", producer => producer
                    .DefaultTopic(topic)
                    .AddMiddlewares(middlewares => middlewares
                        .AddSerializer<NewtonsoftJsonSerializer>()
                        .Add<ActivityPropagationProducerMiddleware>()
                    )
                )
            )
        );

        services.AddOpenTelemetryTracing(builder =>
        {

            builder
                .SetResourceBuilder(ResourceBuilder
                .CreateDefault()
                .AddService("AttributesConfigServiceMock"));

            builder
                .AddSource("kafkaflow");

            builder
                .AddJaegerExporter(exporter =>
                {
                    exporter.AgentHost = "jaeger-all-in-one";
                })
                .AddConsoleExporter();
        });
    })
    .RunConsoleAsync();
