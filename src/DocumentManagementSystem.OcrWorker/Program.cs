using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.OcrWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddHostedService<OcrQueueWorker>();

var host = builder.Build();
host.Run();
