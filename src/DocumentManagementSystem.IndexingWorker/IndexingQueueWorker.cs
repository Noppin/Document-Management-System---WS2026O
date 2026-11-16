using DocumentManagementSystem.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.IndexingWorker;

public sealed class IndexingQueueWorker(
    IOptions<RabbitMqOptions> options,
    OcrCompletedMessageProcessor processor,
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
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RabbitMqTopology.DeclareIndexingConsumerTopologyAsync(channel, _options, stoppingToken);
        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                processor.Process(eventArgs.Body);
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
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Indexing worker failed; message {MessageId} will be requeued",
                    eventArgs.BasicProperties.MessageId);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
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
