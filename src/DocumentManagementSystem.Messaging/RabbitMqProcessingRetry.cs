using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.Messaging;

/// <summary>Retries keep their attempt count across reconnects and worker restarts.</summary>
public static class RabbitMqProcessingRetry
{
    public const string RetryHeader = "x-dms-processing-retries";

    public static async Task HandleAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        string queueName,
        RabbitMqOptions options,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var retries = 0;
        if (delivery.BasicProperties.Headers?.TryGetValue(RetryHeader, out var value) == true)
        {
            retries = value switch
            {
                int number when number >= 0 => number,
                long number when number >= 0 && number <= int.MaxValue => (int)number,
                _ => int.MaxValue // A corrupt counter must not cause an infinite retry loop.
            };
        }

        if (retries >= Math.Clamp(options.MaxProcessingRetries, 0, 100))
        {
            logger.LogError("Processing retries exhausted for message {MessageId}; dead-lettering",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, false, false, cancellationToken);
            return;
        }

        // Delay while the original delivery remains unacknowledged, so shutdown cannot lose it.
        var milliseconds = Math.Clamp(options.ProcessingRetryDelayMilliseconds, 1, 60000);
        await Task.Delay(TimeSpan.FromMilliseconds(milliseconds * Math.Pow(2, Math.Min(retries, 6))),
            cancellationToken);

        var properties = new BasicProperties(delivery.BasicProperties)
        {
            DeliveryMode = DeliveryModes.Persistent,
            Headers = delivery.BasicProperties.Headers is { } headers
                ? new Dictionary<string, object?>(headers)
                : new Dictionary<string, object?>()
        };
        properties.Headers[RetryHeader] = retries + 1;

        try
        {
            // Consumer channels enable publisher confirms. Only ACK the original once the
            // broker confirmed the persistent retry. A crash can duplicate, but not lose, a job.
            await channel.BasicPublishAsync("", queueName, true, properties, delivery.Body, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not confirm retry for message {MessageId}; retaining original",
                delivery.BasicProperties.MessageId);
            await channel.BasicNackAsync(delivery.DeliveryTag, false, true, cancellationToken);
        }
    }
}
