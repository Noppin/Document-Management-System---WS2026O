using System.Text.Json;
using DocumentManagementSystem.Messaging;

namespace DocumentManagementSystem.OcrWorker;

public sealed class OcrMessageProcessor(ILogger<OcrMessageProcessor> logger)
{
    public DocumentUploadedMessage Process(ReadOnlyMemory<byte> body)
    {
        DocumentUploadedMessage message;
        try
        {
            message = JsonSerializer.Deserialize<DocumentUploadedMessage>(body.Span)
                ?? throw new JsonException("The OCR queue message body was empty.");
        }
        catch (JsonException exception)
        {
            throw new OcrMessageProcessingException(
                "The OCR worker could not deserialize the RabbitMQ message.",
                exception);
        }

        if (message.SchemaVersion != DocumentUploadedMessage.CurrentSchemaVersion)
        {
            throw new OcrMessageProcessingException(
                $"The OCR queue message has schema version {message.SchemaVersion}, but version {DocumentUploadedMessage.CurrentSchemaVersion} is supported.");
        }

        if (message.DocumentId == Guid.Empty || string.IsNullOrWhiteSpace(message.FileName))
        {
            throw new OcrMessageProcessingException(
                "The OCR queue message is missing required document data.");
        }

        logger.LogInformation(
            "OCR worker received document {DocumentId} ({FileName}) for later OCR processing",
            message.DocumentId,
            message.FileName);

        return message;
    }
}
