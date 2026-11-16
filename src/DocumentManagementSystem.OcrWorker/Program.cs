using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.OcrWorker;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using DocumentManagementSystem.Storage;
using Minio;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<OcrOptions>(
    builder.Configuration.GetSection(OcrOptions.SectionName));
builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection(MinioOptions.SectionName));
builder.Services.AddSingleton<IMinioClient>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MinioOptions>>().Value;
    return new MinioClient()
        .WithEndpoint(options.Endpoint)
        .WithCredentials(options.AccessKey, options.SecretKey)
        .WithSSL(options.UseSsl)
        .Build();
});
builder.Services.AddSingleton<IDocumentStorage, MinioDocumentStorage>();
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
builder.Services.AddSingleton<IOcrEngine, TesseractOcrEngine>();
builder.Services.AddSingleton<IOcrDocumentProcessor, OcrDocumentProcessor>();
builder.Services.AddHostedService<OcrQueueWorker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>()
    .CreateLogger("DocumentManagementSystem.OcrWorker");
logger.LogInformation("OCR worker starting");

host.Run();
