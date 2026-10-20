using System.Text.Json;
using DocumentManagementSystem.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;

namespace DocumentManagementSystem.OcrWorker.Tests;

public sealed class OcrQueueWorkerTests
{
    private const ulong DeliveryTag = 42;

    private static DocumentUploadedMessage CreateMessage() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "report.pdf",
        "application/pdf",
        42,
        DateTimeOffset.UtcNow);

    private static OcrQueueWorker CreateWorker(IConnectionFactory factory) => new(
        Options.Create(new RabbitMqOptions()),
        new OcrMessageProcessor(NullLogger<OcrMessageProcessor>.Instance),
        NullLogger<OcrQueueWorker>.Instance,
        factory);

    private static IConnectionFactory CreateBroker(IChannel channel)
    {
        var factory = Substitute.For<IConnectionFactory>();
        var connection = Substitute.For<IConnection>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(connection);
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())
            .Returns(channel);
        return factory;
    }

    private static void CaptureConsumer(IChannel channel, Action<IAsyncBasicConsumer> capture)
    {
        channel.BasicConsumeAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<IDictionary<string, object?>>(),
                Arg.Any<IAsyncBasicConsumer>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capture(callInfo.ArgAt<IAsyncBasicConsumer>(6));
                return Task.FromResult("consumer-tag");
            });
    }

    private static async Task<IAsyncBasicConsumer> WaitForConsumerAsync(
        Func<IAsyncBasicConsumer?> accessor)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (accessor() is { } consumer)
            {
                return consumer;
            }

            await Task.Delay(10);
        }

        throw new InvalidOperationException("The OCR worker did not start consuming messages.");
    }

    private static Task DeliverAsync(IAsyncBasicConsumer consumer, byte[] body) =>
        consumer.HandleBasicDeliverAsync(
            "consumer-tag",
            DeliveryTag,
            false,
            "dms.events",
            "document.uploaded",
            new BasicProperties { MessageId = Guid.NewGuid().ToString() },
            body,
            CancellationToken.None);

    [Fact]
    public async Task Worker_ValidMessage_AcknowledgesDelivery()
    {
        var channel = Substitute.For<IChannel>();
        IAsyncBasicConsumer? captured = null;
        CaptureConsumer(channel, consumer => captured = consumer);
        using var worker = CreateWorker(CreateBroker(channel));
        await worker.StartAsync(CancellationToken.None);

        var consumer = await WaitForConsumerAsync(() => captured);
        await DeliverAsync(consumer, JsonSerializer.SerializeToUtf8Bytes(CreateMessage()));

        await channel.Received(1).BasicAckAsync(
            DeliveryTag,
            false,
            Arg.Any<CancellationToken>());
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_InvalidMessage_RejectsWithoutRequeue()
    {
        var channel = Substitute.For<IChannel>();
        IAsyncBasicConsumer? captured = null;
        CaptureConsumer(channel, consumer => captured = consumer);
        using var worker = CreateWorker(CreateBroker(channel));
        await worker.StartAsync(CancellationToken.None);

        var consumer = await WaitForConsumerAsync(() => captured);
        await DeliverAsync(consumer, "not-json"u8.ToArray());

        await channel.Received(1).BasicNackAsync(
            DeliveryTag,
            false,
            false,
            Arg.Any<CancellationToken>());
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_AcknowledgementFails_RequeuesDelivery()
    {
        var channel = Substitute.For<IChannel>();
        channel.BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask>(_ => throw new InvalidOperationException("channel lost"));
        IAsyncBasicConsumer? captured = null;
        CaptureConsumer(channel, consumer => captured = consumer);
        using var worker = CreateWorker(CreateBroker(channel));
        await worker.StartAsync(CancellationToken.None);

        var consumer = await WaitForConsumerAsync(() => captured);
        await DeliverAsync(consumer, JsonSerializer.SerializeToUtf8Bytes(CreateMessage()));

        await channel.Received(1).BasicNackAsync(
            DeliveryTag,
            false,
            true,
            Arg.Any<CancellationToken>());
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Worker_BrokerUnavailable_KeepsRunningAndStopsCleanly()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new InvalidOperationException("broker down")));
        using var worker = CreateWorker(factory);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        Assert.NotEmpty(factory.ReceivedCalls());
    }
}
