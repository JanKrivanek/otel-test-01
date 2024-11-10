using System.Diagnostics.Metrics;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace otel_test_01;

class Program
{
    /// <summary>
    /// Needed so that we can dispose the tracer on exit.
    /// </summary>
    private static TracerProvider tracerHolder = null;

    static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "OTLP-Example");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddTransient<Testing>();
                var otel = services.AddOpenTelemetry();

                // Add Metrics for ASP.NET Core and our custom metrics and export via OTLP
                otel.WithMetrics(metrics =>
                {
                    //Our custom metrics
                    metrics.AddMeter(Testing.MeterName);
                    metrics.AddMeter(Testing.MeterName + "2");
                });

                // Add Tracing for ASP.NET Core and our custom ActivitySource and export via OTLP
                otel.WithTracing(tracing =>
                {
                    tracing.AddSource(Testing.ActivityName);
                });


                if (Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") != null)
                {
                    otel.UseOtlpExporter();
                }

            })
            .ConfigureLogging(loggingCfg => loggingCfg.AddOpenTelemetry(logging =>
                    {
                        logging.IncludeFormattedMessage = true;
                        logging.IncludeScopes = true;
                    }
                )
            )
            .Build();

        Sdk.CreateMeterProviderBuilder().AddMeter(Testing.MeterName, Testing.MeterName + "2").Build();


        var resource = ResourceBuilder
            .CreateDefault()
            .AddService(".NET CLR OpenTelemetry Hook", "2.0.1");

        var tracer =
            Sdk
                .CreateTracerProviderBuilder()
                .SetResourceBuilder(resource);

        
            tracer = tracer.AddSource(Testing.ActivityName);


        tracerHolder =
            tracer
                .AddOtlpExporter()
                .Build();

        // Microsoft.VisualStudio.OpenTelemetry.Collector.

        var my = host.Services.GetRequiredService<Testing>();
        await my.ExecuteAsync();

        tracerHolder.Dispose();
    }
}

class Testing
{
    public const string MeterName = "OTel.Example";
    public const string ActivityName = "OTel.ExampleAct";
    private readonly ILogger<Testing> _logger;

    public Testing(ILogger<Testing> logger)
    {
        _logger = logger;
        Init();
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Doing something");

        while (Console.ReadKey(false).Key != ConsoleKey.Q)
        {

            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
            SendGreeting(_logger);
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
            Foo();
            await Task.Delay(100, stoppingToken).ConfigureAwait(false);
        }

        Console.WriteLine("Bye!");
    }

    private void Foo()
    {
        countGreetings2.Add(new Random().NextDouble() * 10.0);

        SendGreeting(_logger);
    }

    private Meter greeterMeter;
    private Meter greeterMeter2;
    private Counter<int> countGreetings;
    private Counter<double> countGreetings2;

    // Custom ActivitySource for the application
    private ActivitySource greeterActivitySource;

    public void Init()
    {
        // Custom metrics for the application
        greeterMeter = new Meter(MeterName, "1.0.0");
        greeterMeter2 = new Meter(MeterName + "2", "1.0.0");
        countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");
        countGreetings2 = greeterMeter2.CreateCounter<double>("another.count", description: "Counts smth");
        
        greeterActivitySource = new ActivitySource(ActivityName);
    }

    public string SendGreeting(ILogger<Testing> logger)
    {
        // Create a new Activity scoped to the method
        using var activity = greeterActivitySource.StartActivity("GreeterActivity");

        // Log a message
        logger.LogInformation("Sending greeting");

        // Increment the custom counter
        countGreetings.Add(1); ;

        // Add a tag to the Activity
        activity?.SetTag("greeting", "Hello World!");

        return "Hello World!";
    }
}
