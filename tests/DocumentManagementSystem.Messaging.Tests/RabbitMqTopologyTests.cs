using NSubstitute;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Messaging.Tests;

public sealed class RabbitMqTopologyTests
{
    private static readonly RabbitMqOptions Options = new();
    private readonly IChannel _channel = Substitute.For<IChannel>();

    [Fact]
    public async Task DeclareProducerTopologyAsync_DeclaresDurableTopicExchange()
    {
        await RabbitMqTopology.DeclareProducerTopologyAsync(_channel, Options);

        await _channel.Received(1).ExchangeDeclareAsync(
            Arg.Is<string>(name => name == "dms.events"),
            Arg.Is<string>(type => type == "topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeclareConsumerTopologyAsync_DeclaresDeadLetterTopology()
    {
        await RabbitMqTopology.DeclareConsumerTopologyAsync(_channel, Options);

        await _channel.Received(1).ExchangeDeclareAsync(
            Arg.Is<string>(name => name == "dms.events"),
            Arg.Is<string>(type => type == "topic"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        await _channel.Received(1).ExchangeDeclareAsync(
            Arg.Is<string>(name => name == "dms.events.dlx"),
            Arg.Is<string>(type => type == "fanout"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        await _channel.Received(1).QueueDeclareAsync(
            Arg.Is<string>(queue => queue == "dms.document.ocr.dead"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Is(false),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        await _channel.Received(1).QueueBindAsync(
            Arg.Is<string>(queue => queue == "dms.document.ocr.dead"),
            Arg.Is<string>(exchange => exchange == "dms.events.dlx"),
            Arg.Is<string>(routingKey => routingKey == string.Empty),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        await _channel.Received(1).QueueDeclareAsync(
            Arg.Is<string>(queue => queue == "dms.document.ocr"),
            Arg.Is(true),
            Arg.Is(false),
            Arg.Is(false),
            Arg.Is<IDictionary<string, object?>>(arguments =>
                arguments.ContainsKey(RabbitMqTopology.DeadLetterExchangeArgument)
                && (string?)arguments[RabbitMqTopology.DeadLetterExchangeArgument] == "dms.events.dlx"),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());

        await _channel.Received(1).QueueBindAsync(
            Arg.Is<string>(queue => queue == "dms.document.ocr"),
            Arg.Is<string>(exchange => exchange == "dms.events"),
            Arg.Is<string>(routingKey => routingKey == "document.uploaded"),
            Arg.Any<IDictionary<string, object?>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DeadLetterExchangeArgument_UsesRabbitMqConvention()
    {
        Assert.Equal("x-dead-letter-exchange", RabbitMqTopology.DeadLetterExchangeArgument);
    }
}
