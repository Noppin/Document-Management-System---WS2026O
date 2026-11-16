namespace DocumentManagementSystem.Storage;

public static class DocumentObjectName
{
    public static string For(Guid documentId) => $"documents/{documentId:N}.pdf";
}
