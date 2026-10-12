using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Application;
using DocumentManagementSystem.Api.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("DocumentManagementDb")
	?? throw new InvalidOperationException("Connection string 'DocumentManagementDb' is not configured.");
builder.Services.AddPersistence(connectionString);
builder.Services.AddApplication();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();

app.Services.ApplyMigrations();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();
app.Run();
