namespace DocumentManagementSystem.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "dms";
    public string Password { get; init; } = "dms";
    public string QueueName { get; init; } = "dms.document.ocr";
    public string ExchangeName { get; init; } = "dms.events";
    public string RoutingKey { get; init; } = "document.uploaded";
    public string DeadLetterExchangeName { get; init; } = "dms.events.dlx";
    public string DeadLetterQueueName { get; init; } = "dms.document.ocr.dead";
    public string IndexQueueName { get; init; } = "dms.document.index";
    public string IndexRoutingKey { get; init; } = "document.ocr.completed";
    public string IndexDeadLetterExchangeName { get; init; } = "dms.events.index.dlx";
    public string IndexDeadLetterQueueName { get; init; } = "dms.document.index.dead";
    public int MaxProcessingRetries { get; init; } = 3;
    public int ProcessingRetryDelayMilliseconds { get; init; } = 1000;
}
