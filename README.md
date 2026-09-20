Document Management System

ASP.NET Core Web API for document uploads.

## Requirements

- .NET 10 SDK

Check the installed SDK:

```powershell
dotnet --list-sdks
```

## Restore and build

Run from the repository root:

```powershell
dotnet restore DocumentManagementSystem.sln
dotnet build DocumentManagementSystem.sln
```

## Run the API

### Development

In [launchSettings.json](src/DocumentManagementSystem.Api/Properties/launchSettings.json), change:

```json
"ASPNETCORE_ENVIRONMENT": "Production"
```

to:

```json
"ASPNETCORE_ENVIRONMENT": "Development"
```

Then start the API:

```powershell
dotnet run --project src\DocumentManagementSystem.Api\DocumentManagementSystem.Api.csproj
```

Open Swagger at:

```text
http://localhost:5000/swagger
```

### Production

Swagger is disabled in Production:

```powershell
dotnet run --project src\DocumentManagementSystem.Api\DocumentManagementSystem.Api.csproj
```

Restart the API after changing the environment.

## Endpoints

### Health check

```powershell
Invoke-WebRequest http://localhost:5000/health -UseBasicParsing
```

Expected status: `200 OK`.

### Upload a PDF with Swagger

1. Open `http://localhost:5000/swagger` in Development.
2. Expand `POST /documents`.
3. Click **Try it out**.
4. Select a PDF using **Choose File**.
5. Click **Execute**.

Valid uploads must be non-empty PDF files with the `application/pdf` content type and a `%PDF-` header. Successful uploads return `201 Created`; invalid uploads return `400 Bad Request`.

## Environment settings

The default environment is configured in [launchSettings.json](src/DocumentManagementSystem.Api/Properties/launchSettings.json). The application enables Swagger only when `ASPNETCORE_ENVIRONMENT` is `Development`.


Stop the API with `Ctrl+C`.