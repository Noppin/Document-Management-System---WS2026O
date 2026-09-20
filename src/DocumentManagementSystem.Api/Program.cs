using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Api.Contracts;
using DocumentManagementSystem.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddPersistence();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/documents", async (IFormFile file, CancellationToken cancellationToken) =>
{
	if (file.Length == 0 ||
		!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
		!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
	{
		return Results.BadRequest(new { error = "Only non-empty PDF files are accepted." });
	}

	await using var stream = file.OpenReadStream();
	var header = new byte[5];
	var bytesRead = await stream.ReadAsync(header, cancellationToken);
	if (bytesRead != header.Length || !header.SequenceEqual("%PDF-"u8.ToArray()))
	{
		return Results.BadRequest(new { error = "The uploaded file is not a valid PDF." });
	}

	var document = new Document
	{
		FileName = Path.GetFileName(file.FileName),
		ContentType = file.ContentType,
		FileSize = file.Length
	};

	return Results.Created($"/documents/{document.Id}", new DocumentResponse(
		document.Id,
		document.FileName,
		document.ContentType,
		document.FileSize,
		document.CreatedAt));
}).Accepts<IFormFile>("multipart/form-data").DisableAntiforgery();

app.Run();
