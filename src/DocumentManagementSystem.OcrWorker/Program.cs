using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.OcrWorker;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IConnectionFactory>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
    return new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
        ClientProvidedName = "dms-ocr-worker"
    };
});
builder.Services.AddSingleton<OcrMessageProcessor>();
builder.Services.AddHostedService<OcrQueueWorker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("DocumentManagementSystem.OcrWorker");
logger.LogInformation("OCR worker starting");

host.Run();
