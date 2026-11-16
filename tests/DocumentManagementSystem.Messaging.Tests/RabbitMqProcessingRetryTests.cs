// Mit AI geschrieben.
using DocumentManagementSystem.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentManagementSystem.Messaging.Tests;

public sealed class RabbitMqProcessingRetryTests
{
    [Fact]
    public async Task Retry_PublishesPersistentMessageBeforeAcknowledgingOriginal()
    {
        var channel = Substitute.For<IChannel>();
        var delivery = Delivery();
        await RabbitMqProcessingRetry.HandleAsync(channel, delivery, "jobs", Options(),
            NullLogger.Instance, CancellationToken.None);

        Received.InOrder(() =>
        {
            channel.BasicPublishAsync("", "jobs", true,
                Arg.Is<BasicProperties>(properties => properties.Persistent &&
                    (int)properties.Headers![RabbitMqProcessingRetry.RetryHeader]! == 1),
                delivery.Body, Arg.Any<CancellationToken>());
            channel.BasicAckAsync(42, false, Arg.Any<CancellationToken>());
        });
    }

    [Theory]
    [InlineData(3)]
    [InlineData(100)]
    public async Task Retry_ExhaustedMessageIsDeadLettered(int retries)
    {
        var channel = Substitute.For<IChannel>();
        await RabbitMqProcessingRetry.HandleAsync(channel, Delivery(retries), "jobs", Options(),
            NullLogger.Instance, CancellationToken.None);
        await channel.Received(1).BasicNackAsync(42, false, false, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retry_FailedPublishRetainsOriginal()
    {
        var channel = Substitute.For<IChannel>();
        channel.BasicPublishAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<BasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new InvalidOperationException("broker down"));

        await RabbitMqProcessingRetry.HandleAsync(channel, Delivery(), "jobs", Options(),
            NullLogger.Instance, CancellationToken.None);

        await channel.Received(1).BasicNackAsync(42, false, true, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retry_ShutdownDoesNotAcknowledgeOriginal()
    {
        var channel = Substitute.For<IChannel>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RabbitMqProcessingRetry.HandleAsync(
            channel, Delivery(), "jobs", Options(), NullLogger.Instance, cancellation.Token));
        Assert.Empty(channel.ReceivedCalls());
    }

    private static RabbitMqOptions Options() => new() { ProcessingRetryDelayMilliseconds = 1 };

    private static BasicDeliverEventArgs Delivery(int retries = 0) => new(
        "consumer", 42, false, "events", "upload",
        new BasicProperties
        {
            MessageId = "message-1",
            Headers = new Dictionary<string, object?> { [RabbitMqProcessingRetry.RetryHeader] = retries }
        }, "job"u8.ToArray(), CancellationToken.None);
}
