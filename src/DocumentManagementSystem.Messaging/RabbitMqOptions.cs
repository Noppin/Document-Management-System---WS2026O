namespace DocumentManagementSystem.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "dms";
    public string Password { get; init; } = "dms";
    public string QueueName { get; init; } = "dms.document.ocr";
}
