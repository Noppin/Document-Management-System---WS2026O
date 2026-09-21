using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddPersistence();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/collections", (CreateCollectionRequest request, ICollectionRepository collections) =>
{
	if (string.IsNullOrWhiteSpace(request.Name))
	{
		return Results.BadRequest(new { error = "A collection name is required." });
	}

	var collection = collections.Add(new Collection { Name = request.Name.Trim() });

	return Results.Created($"/collections/{collection.Id}", ToCollectionResponse(collection));
});

app.MapGet("/collections", (ICollectionRepository collections) =>
	Results.Ok(collections.GetAll().Select(ToCollectionResponse).ToList()));

app.MapGet("/collections/{id:guid}", (Guid id, ICollectionRepository collections, IDocumentRepository documents) =>
{
	var collection = collections.GetById(id);
	if (collection is null)
	{
		return Results.NotFound();
	}

	var collectionDocuments = collection.CollectionDocuments
		.Select(link => documents.GetById(link.DocumentId))
		.OfType<Document>()
		.Select(ToDocumentResponse)
		.ToList();

	return Results.Ok(new CollectionDetailResponse(
		collection.Id,
		collection.Name,
		collection.CreatedAt,
		collectionDocuments));
});

app.MapPost("/collections/{id:guid}/documents", (Guid id, AssignDocumentRequest request, ICollectionRepository collections, IDocumentRepository documents) =>
{
	if (documents.GetById(request.DocumentId) is null)
	{
		return Results.NotFound(new { error = "Document not found." });
	}

	if (!collections.AddDocument(id, request.DocumentId))
	{
		return Results.NotFound(new { error = "Collection not found." });
	}

	return Results.NoContent();
});

app.MapDelete("/collections/{id:guid}/documents/{documentId:guid}", (Guid id, Guid documentId, ICollectionRepository collections) =>
{
	if (!collections.RemoveDocument(id, documentId))
	{
		return Results.NotFound();
	}

	return Results.NoContent();
});

app.MapControllers();
app.Run();

static CollectionResponse ToCollectionResponse(Collection collection) =>
	new(collection.Id, collection.Name, collection.CreatedAt, collection.CollectionDocuments.Count);

static DocumentResponse ToDocumentResponse(Document document) =>
	new(document.Id, document.FileName, document.ContentType, document.FileSize, document.CreatedAt);
