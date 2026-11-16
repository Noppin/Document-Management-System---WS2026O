using DocumentManagementSystem.IndexingWorker;
using DocumentManagementSystem.Messaging;
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
        ClientProvidedName = "dms-indexing-worker"
    };
});
builder.Services.AddSingleton<OcrCompletedMessageProcessor>();
builder.Services.AddHostedService<IndexingQueueWorker>();

var host = builder.Build();

host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("DocumentManagementSystem.IndexingWorker")
    .LogInformation("Indexing worker starting");

host.Run();
