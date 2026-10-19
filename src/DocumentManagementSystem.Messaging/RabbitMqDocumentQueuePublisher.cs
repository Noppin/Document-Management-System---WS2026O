using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Messaging;

public sealed class RabbitMqDocumentQueuePublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqDocumentQueuePublisher> logger) : IDocumentQueuePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
        DocumentUploadedMessage message,
        CancellationToken cancellationToken = default)
    {
        await _channelLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureChannelAsync(cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.MessageId.ToString(),
                Type = nameof(DocumentUploadedMessage)
            };

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Published OCR queue message {MessageId} for document {DocumentId}",
                message.MessageId,
                message.DocumentId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to publish OCR queue message for document {DocumentId}",
                message.DocumentId);
            throw new DocumentQueueException(
                $"Could not enqueue document {message.DocumentId} for OCR processing.",
                exception);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "dms-api-publisher"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _channelLock.Dispose();
    }
}
