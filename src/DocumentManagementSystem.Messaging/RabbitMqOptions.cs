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
}
