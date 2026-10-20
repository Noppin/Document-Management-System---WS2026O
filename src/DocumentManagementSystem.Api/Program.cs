using DocumentManagementSystem.Persistence;
using DocumentManagementSystem.Application;
using DocumentManagementSystem.Api.ExceptionHandling;
using DocumentManagementSystem.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ArgumentExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DocumentQueueExceptionHandler>();
var connectionString = builder.Configuration.GetConnectionString("DocumentManagementDb")
	?? throw new InvalidOperationException("Connection string 'DocumentManagementDb' is not configured.");
builder.Services.AddPersistence(connectionString);
builder.Services.AddApplication();
builder.Services.Configure<RabbitMqOptions>(
	builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IConnectionFactory>(serviceProvider =>
{
	var options = serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
	return new ConnectionFactory
	{
		HostName = options.HostName,
		Port = options.Port,
		UserName = options.UserName,
		Password = options.Password,
		AutomaticRecoveryEnabled = true,
		TopologyRecoveryEnabled = true,
		NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
		ClientProvidedName = "dms-api-publisher"
	};
});
builder.Services.AddSingleton<IDocumentQueuePublisher, RabbitMqDocumentQueuePublisher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Keycloak access token.",
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer",
				},
			},
			Array.Empty<string>()
		},
	});
});
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.Authority = builder.Configuration["Keycloak:Authority"]
			?? throw new InvalidOperationException("Keycloak authority is not configured.");
		options.Audience = builder.Configuration["Keycloak:Audience"];
		options.RequireHttpsMetadata = false;
	});
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

app.Services.ApplyMigrations();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();
app.Run();
