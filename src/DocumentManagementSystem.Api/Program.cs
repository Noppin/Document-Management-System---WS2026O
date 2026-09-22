using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Api.Services;
using DocumentManagementSystem.Api.ExceptionHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("DocumentManagementDb")
	?? throw new InvalidOperationException("Connection string 'DocumentManagementDb' is not configured.");
builder.Services.AddPersistence(connectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementDbContext>();
	dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();
app.Run();
