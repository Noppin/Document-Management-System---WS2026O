// Mit AI geschrieben.
using System.Text.Json;
using DocumentManagementSystem.IndexingWorker;
using DocumentManagementSystem.Messaging;
using DocumentManagementSystem.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RabbitMQ.Client;

namespace DocumentManagementSystem.OcrWorker.Tests;

public sealed class IndexingQueueWorkerTests
{
    [Fact]
    public async Task Worker_IndexesMessageBeforeAck()
    {
        var index = Substitute.For<IDocumentSearchIndex>();
        var message = new OcrCompletedMessage(Guid.NewGuid(), Guid.NewGuid(), "a.pdf", "Hello World", DateTimeOffset.UtcNow);
        var (worker, channel, consumer) = await StartAsync(index);
        using (worker)
        {
            await DeliverAsync(consumer, JsonSerializer.SerializeToUtf8Bytes(message));
            Received.InOrder(() =>
            {
                index.IndexAsync(Arg.Is<SearchDocument>(document =>
                    document.DocumentId == message.DocumentId && document.Content == "Hello World"), Arg.Any<CancellationToken>());
                channel.BasicAckAsync(42, false, Arg.Any<CancellationToken>());
            });
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_ElasticsearchFailureDeadLettersAfterRetryBudget()
    {
        var index = Substitute.For<IDocumentSearchIndex>();
        index.IndexAsync(Arg.Any<SearchDocument>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new SearchIndexException("Elasticsearch down")));
        var (worker, channel, consumer) = await StartAsync(index);
        using (worker)
        {
            await DeliverAsync(consumer, JsonSerializer.SerializeToUtf8Bytes(new OcrCompletedMessage(
                Guid.NewGuid(), Guid.NewGuid(), "a.pdf", "text", DateTimeOffset.UtcNow)), 3);
            await channel.Received(1).BasicNackAsync(42, false, false, Arg.Any<CancellationToken>());
            await channel.DidNotReceive().BasicAckAsync(42, false, Arg.Any<CancellationToken>());
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task Worker_InvalidPayloadIsDeadLetteredWithoutIndexing()
    {
        var index = Substitute.For<IDocumentSearchIndex>();
        var (worker, channel, consumer) = await StartAsync(index);
        using (worker)
        {
            await DeliverAsync(consumer, "invalid-json"u8.ToArray());
            await channel.Received(1).BasicNackAsync(42, false, false, Arg.Any<CancellationToken>());
            await index.DidNotReceive().IndexAsync(Arg.Any<SearchDocument>(), Arg.Any<CancellationToken>());
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<(IndexingQueueWorker Worker, IChannel Channel, IAsyncBasicConsumer Consumer)> StartAsync(
        IDocumentSearchIndex index)
    {
        var channel = Substitute.For<IChannel>();
        var connection = Substitute.For<IConnection>();
        var factory = Substitute.For<IConnectionFactory>();
        factory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(connection);
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>()).Returns(channel);
        var ready = new TaskCompletionSource<IAsyncBasicConsumer>(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.BasicConsumeAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<bool>(),
                Arg.Any<bool>(), Arg.Any<IDictionary<string, object?>>(), Arg.Any<IAsyncBasicConsumer>(), Arg.Any<CancellationToken>())
            .Returns(call => { ready.TrySetResult(call.ArgAt<IAsyncBasicConsumer>(6)); return Task.FromResult("consumer"); });
        var worker = new IndexingQueueWorker(
            Options.Create(new RabbitMqOptions { ProcessingRetryDelayMilliseconds = 1 }),
            new OcrCompletedMessageProcessor(NullLogger<OcrCompletedMessageProcessor>.Instance),
            index, NullLogger<IndexingQueueWorker>.Instance, factory);
        await worker.StartAsync(CancellationToken.None);
        return (worker, channel, await ready.Task.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static Task DeliverAsync(IAsyncBasicConsumer consumer, byte[] body, int retries = 0) =>
        consumer.HandleBasicDeliverAsync("consumer", 42, false, "events", "ocr.completed",
            new BasicProperties { Headers = new Dictionary<string, object?> { [RabbitMqProcessingRetry.RetryHeader] = retries } },
            body, CancellationToken.None);
}
