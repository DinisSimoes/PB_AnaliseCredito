using MassTransit;
using PB_AnaliseCredito;
using PB_AnaliseCredito.Consumer;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

// OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("PB_AnaliseCredito.Worker"))
            .AddSource("PB_AnaliseCredito.Worker")
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(configuration["OpenTelemetry:OtlpEndpoint"] ?? "");
            });
    });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AnaliseCreditoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // configura host do rabbit
        cfg.Host(configuration["RabbitMQ:Host"], 5672, "/", h => {
            h.Username(configuration["RabbitMQ:Username"] ?? "");
            h.Password(configuration["RabbitMQ:Password"] ?? "");
        });

        //configura fila que vai ficar ouvindo
        cfg.ReceiveEndpoint("analise-credito-queue", e => 
        {
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
            e.ConfigureConsumer<AnaliseCreditoConsumer>(context);
        });
    });
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();