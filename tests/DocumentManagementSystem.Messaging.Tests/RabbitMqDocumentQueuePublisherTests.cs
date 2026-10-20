using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;

namespace DocumentManagementSystem.Messaging.Tests;

public sealed class RabbitMqDocumentQueuePublisherTests
{
    private static DocumentUploadedMessage CreateMessage() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "report.pdf",
        "application/pdf",
        42,
        DateTimeOffset.UtcNow);

    private static RabbitMqDocumentQueuePublisher CreatePublisher(IConnectionFactory factory) => new(
        Options.Create(new RabbitMqOptions()),
        NullLogger<RabbitMqDocumentQueuePublisher>.Instance,
        factory);

    private static (IConnectionFactory Factory, IConnection Connection, IChannel Channel) CreateBroker()
    {
        var factory = Substitute.For<IConnectionFactory>();
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IChannel>();

        factory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(connection);
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())
            .Returns(channel);

        return (factory, connection, channel);
    }

    [Fact]
    public async Task PublishAsync_ValidMessage_DeclaresTopologyAndPublishesPersistentMessage()
    {
        var (factory, _, channel) = CreateBroker();
        var message = CreateMessage();
        await using var publisher = CreatePublisher(factory);

        await publisher.PublishAsync(message);

        var declareCall = Assert.Single(
            channel.ReceivedCalls(),
            call => call.GetMethodInfo().Name == "ExchangeDeclareAsync");
        var declareArguments = declareCall.GetArguments();
        Assert.Equal("dms.events", declareArguments[0]);
        Assert.Equal("topic", declareArguments[1]);
        Assert.Equal(true, declareArguments[2]);

        var publishCall = Assert.Single(
            channel.ReceivedCalls(),
            call => call.GetMethodInfo().Name == "BasicPublishAsync");
        var publishArguments = publishCall.GetArguments();
        Assert.Equal("dms.events", publishArguments[0]);
        Assert.Equal("document.uploaded", publishArguments[1]);
        Assert.Equal(true, publishArguments[2]);

        var properties = Assert.IsType<BasicProperties>(publishArguments[3]);
        Assert.Equal(message.MessageId.ToString(), properties.MessageId);
        Assert.Equal("application/json", properties.ContentType);
        Assert.Equal(nameof(DocumentUploadedMessage), properties.Type);
        Assert.True(properties.Persistent);

        var body = Assert.IsType<ReadOnlyMemory<byte>>(publishArguments[4]);
        var published = JsonSerializer.Deserialize<DocumentUploadedMessage>(body.Span);
        Assert.NotNull(published);
        Assert.Equal(message.DocumentId, published!.DocumentId);
        Assert.Equal(DocumentUploadedMessage.CurrentSchemaVersion, published.SchemaVersion);
    }

    [Fact]
    public async Task PublishAsync_Twice_ReusesOpenConnectionAndChannel()
    {
        var (factory, connection, channel) = CreateBroker();
        connection.IsOpen.Returns(true);
        channel.IsOpen.Returns(true);
        var message = CreateMessage();
        await using var publisher = CreatePublisher(factory);

        await publisher.PublishAsync(message);
        await publisher.PublishAsync(message);

        await factory.Received(1).CreateConnectionAsync(Arg.Any<CancellationToken>());
        await channel.Received(2).BasicPublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_BrokerUnavailable_WrapsExceptionInDocumentQueueException()
    {
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IConnection>(new InvalidOperationException("broker down")));
        await using var publisher = CreatePublisher(factory);

        var exception = await Assert.ThrowsAsync<DocumentQueueException>(
            () => publisher.PublishAsync(CreateMessage()));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task PublishAsync_CancelledToken_PropagatesCancellationWithoutWrapping()
    {
        var factory = Substitute.For<IConnectionFactory>();
        await using var publisher = CreatePublisher(factory);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => publisher.PublishAsync(CreateMessage(), cancellation.Token));

        _ = factory.DidNotReceive().CreateConnectionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisposeAsync_DisposesOpenChannelAndConnection()
    {
        var (factory, connection, channel) = CreateBroker();
        var publisher = CreatePublisher(factory);
        await publisher.PublishAsync(CreateMessage());

        await publisher.DisposeAsync();

        await channel.Received(1).DisposeAsync();
        await connection.Received(1).DisposeAsync();
    }
}
