using DocumentManagementSystem.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DocumentManagementSystem.Search;

namespace DocumentManagementSystem.IndexingWorker;

public sealed class IndexingQueueWorker(
    IOptions<RabbitMqOptions> options,
    OcrCompletedMessageProcessor processor,
    IDocumentSearchIndex searchIndex,
    ILogger<IndexingQueueWorker> logger,
    IConnectionFactory? connectionFactory = null) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerSessionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Indexing worker lost its RabbitMQ consumer session. Retrying in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task RunConsumerSessionAsync(CancellationToken stoppingToken)
    {
        var factory = connectionFactory ?? new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName = "dms-indexing-worker"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await RabbitMqTopology.DeclareIndexingConsumerTopologyAsync(channel, _options, stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = processor.Process(eventArgs.Body);
                await searchIndex.IndexAsync(
                    new SearchDocument(
                        message.DocumentId,
                        message.FileName,
                        message.Text,
                        DateTimeOffset.UtcNow),
                    stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, stoppingToken);
            }
            catch (OcrCompletedMessageProcessingException exception)
            {
                logger.LogError(
                    exception,
                    "Rejecting invalid indexing queue message {MessageId}",
                    eventArgs.BasicProperties.MessageId);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Closing the channel returns the unacknowledged delivery to RabbitMQ.
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Indexing worker failed; applying bounded retry policy to message {MessageId}",
                    eventArgs.BasicProperties.MessageId);
                await RabbitMqProcessingRetry.HandleAsync(
                    channel, eventArgs, _options.IndexQueueName, _options, logger, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _options.IndexQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "Indexing worker is listening on RabbitMQ queue {QueueName}",
            _options.IndexQueueName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
