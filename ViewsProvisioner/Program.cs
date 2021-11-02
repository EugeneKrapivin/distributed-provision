﻿using Microsoft.Extensions.Hosting;
using KafkaFlow;
using KafkaFlow.TypedHandler;
using ViewsProvisionerMock;
using SharedMiddleware;

using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OrleansOpenTelemetryShim;
using Microsoft.Extensions.DependencyInjection;

const string groupId = "views-provisioner";

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
                        .AddSerializer<KafkaFlow.Serializer.NewtonsoftJsonSerializer>()
                        .Add<ActivityPropagationConsumerMiddleware>()
                        .AddTypedHandlers(handlers => handlers
                            .AddHandler<ViewsProvisioner>()
                        )
                    )
                ).AddProducer("producer", producer => producer
                    .DefaultTopic(topic)
                    .AddMiddlewares(middlewares => middlewares
                        .AddSerializer<KafkaFlow.Serializer.NewtonsoftJsonSerializer>()
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
                .AddService("ViewsProvisionerMock"));

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