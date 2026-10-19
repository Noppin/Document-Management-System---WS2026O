using System.Text.Json;
using DocumentManagementSystem.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrQueueWorker(
    IOptions<RabbitMqOptions> options,
    ILogger<OcrQueueWorker> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "dms-ocr-worker"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var message = JsonSerializer.Deserialize<DocumentUploadedMessage>(eventArgs.Body.ToArray())
                ?? throw new InvalidOperationException("RabbitMQ delivered an empty OCR message.");

            logger.LogInformation(
                "OCR worker received document {DocumentId} ({FileName}) for later OCR processing",
                message.DocumentId,
                message.FileName);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "OCR worker is listening on RabbitMQ queue {QueueName}",
            _options.QueueName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
