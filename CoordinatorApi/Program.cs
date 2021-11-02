using CoordinatorApi;
using CoordinatorApi.Orchestrators;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System.Runtime.InteropServices;

using Orleans;
using Orleans.Hosting;
using Orleans.Statistics;
using Orleans.Configuration;

using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

using KafkaFlow;
using KafkaFlow.TypedHandler;

using Serilog;
using Serilog.Sinks.Loki;
using Serilog.Formatting.Compact;
using OrleansOpenTelemetryShim;

const string groupId = "business-unit-service-coordinator";

const string topic = "business-unit-provisioning";

string[] brokers = new[] { "broker:9092" };

await Host.CreateDefaultBuilder(args)
    .UseOrleans((ctx, siloBuilder) =>
    {
        var redisAddress = "redis:6379";
        siloBuilder
            .AddOutgoingGrainCallFilter<ActivityPropagationOutgoingGrainCallFilter>()
            .AddIncomingGrainCallFilter<ActivityPropagationIncomingGrainCallFilter>()
            .UseRedisClustering(redisAddress)
            .ConfigureEndpoints(siloPort: 11111, gatewayPort: 30000)
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "develop";
                options.ServiceId = "business-unit-provisioner-gateway";
            })
            .AddRedisGrainStorage("provision-store", redis =>
            {
                redis.ConnectionString = redisAddress;
            })
            .AddRedisGrainStorageAsDefault(redis =>
            {
                redis.ConnectionString = redisAddress;
            })
            .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
            .UseDashboard(opts => 
            {
                opts.Username = "admin";
                opts.Password = "admin";
                opts.Host = "*";
                opts.Port = 8282;
                opts.HostSelf = false;
                opts.CounterUpdateIntervalMs = 1000;
            });

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            siloBuilder.UsePerfCounterEnvironmentStatistics();
        }
        else
        {
            siloBuilder.UseLinuxEnvironmentStatistics();
        }
    })
    .ConfigureServices(services =>
    {
        services.AddOpenTelemetryMetrics();
        services.AddSingleton<BusinessUnitProvisioningOrchestrator>();
        services.AddOpenTelemetryTracing(builder => 
        {
            
            builder
                .SetResourceBuilder(ResourceBuilder
                .CreateDefault()
                .AddService("CoordinatorApi"));
            
            builder
                .AddSource("orleans.runtime.graincall")
                .AddSource("kafkaflow");

            builder
                .AddAspNetCoreInstrumentation()
                .AddJaegerExporter(exporter =>
                 {
                     exporter.AgentHost = "jaeger-all-in-one";
                 })
                .AddConsoleExporter();
        });
    })
    .ConfigureServices(services =>
    {
        services.AddKafkaFlowHostedService(kafka => kafka
            .AddCluster(cluster => cluster
                .WithBrokers(brokers)
                .WithSecurityInformation(sec => sec.SecurityProtocol = KafkaFlow.Configuration.SecurityProtocol.Plaintext)
                .AddConsumer(consumer => consumer
                    .Topic(topic)
                    .WithGroupId(groupId)
                    .WithBufferSize(100)
                    .WithWorkersCount(10)
                    .AddMiddlewares(middlewares => middlewares
                        .AddSerializer<KafkaFlow.Serializer.NewtonsoftJsonSerializer>()
                        .Add<ActivityPropagationConsumerMiddleware>()
                        .AddTypedHandlers(handlers => handlers
                            .AddHandler<BusinessUnitProvisioningOrchestrator>()
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
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    })
    .UseSerilog((ctx, cfg) =>
    {
        var credentials = new NoAuthCredentials("http://172.18.0.12:3100");

        cfg
            //.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", ctx.HostingEnvironment.ApplicationName)
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
            .WriteTo.LokiHttp(credentials);

        if (ctx.HostingEnvironment.IsDevelopment())
            cfg.WriteTo.Console(new RenderedCompactJsonFormatter());
    })
    .RunConsoleAsync();