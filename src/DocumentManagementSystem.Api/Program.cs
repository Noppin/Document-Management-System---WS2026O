using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Application;
using DocumentManagementSystem.Api.ExceptionHandling;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("DocumentManagementDb")
	?? throw new InvalidOperationException("Connection string 'DocumentManagementDb' is not configured.");
builder.Services.AddPersistence(connectionString);
builder.Services.AddApplication();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<DocumentManagementDbContext>();
	dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();
app.Run();
