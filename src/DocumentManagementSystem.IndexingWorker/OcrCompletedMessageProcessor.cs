using System.Text.Json;
using DocumentManagementSystem.Messaging;

namespace DocumentManagementSystem.IndexingWorker;

public sealed class OcrCompletedMessageProcessor(ILogger<OcrCompletedMessageProcessor> logger)
{
    public OcrCompletedMessage Process(ReadOnlyMemory<byte> body)
    {
        OcrCompletedMessage message;
        try
        {
            message = JsonSerializer.Deserialize<OcrCompletedMessage>(body.Span)
                ?? throw new JsonException("The indexing queue message body was empty.");
        }
        catch (JsonException exception)
        {
            throw new OcrCompletedMessageProcessingException(
                "The indexing worker could not deserialize the OCR result message.",
                exception);
        }

        if (message.SchemaVersion != OcrCompletedMessage.CurrentSchemaVersion)
        {
            throw new OcrCompletedMessageProcessingException(
                $"The OCR result has schema version {message.SchemaVersion}, but version {OcrCompletedMessage.CurrentSchemaVersion} is supported.");
        }

        if (message.DocumentId == Guid.Empty || string.IsNullOrWhiteSpace(message.FileName))
        {
            throw new OcrCompletedMessageProcessingException(
                "The OCR result message is missing required document data.");
        }

        logger.LogInformation(
            "Indexing worker received OCR result for document {DocumentId} ({FileName})",
            message.DocumentId,
            message.FileName);

        return message;
    }
}
