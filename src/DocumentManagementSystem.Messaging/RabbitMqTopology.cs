using RabbitMQ.Client;

namespace DocumentManagementSystem.Messaging;

public static class RabbitMqTopology
{
    public const string DeadLetterExchangeArgument = "x-dead-letter-exchange";

    public static Task DeclareProducerTopologyAsync(
        IChannel channel,
        RabbitMqOptions options,
        CancellationToken cancellationToken = default)
    {
        return channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    public static async Task DeclareConsumerTopologyAsync(
        IChannel channel,
        RabbitMqOptions options,
        CancellationToken cancellationToken = default)
    {
        await DeclareProducerTopologyAsync(channel, options, cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchange: options.DeadLetterExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: options.DeadLetterQueueName,
            exchange: options.DeadLetterExchangeName,
            routingKey: string.Empty,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                [DeadLetterExchangeArgument] = options.DeadLetterExchangeName
            },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
