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

## Start with Docker Compose

Docker Desktop must be running. From the repository root, build and start the
PostgreSQL database and REST API:

```powershell
docker compose build
docker compose up -d
```

Production shortcut:

```powershell
docker compose up -d --build
```

The PostgreSQL container uses the persistent `postgres-data` volume. Uploaded
documents therefore remain available after the API container is restarted.

Check the running services:

```powershell
docker compose ps
Invoke-WebRequest http://localhost:8080/health -UseBasicParsing
```

The expected health response is `200 OK` with `{"status":"ok"}`.

Open the API at:

```text
http://localhost:8080
```

Swagger is disabled in the Docker Production environment. Use the HTTP client
or curl requests in the `http` folder to call the endpoints.

Stop the containers while keeping the database volume:

```powershell
docker compose down
```

Remove the database volume as well:

```powershell
docker compose down -v
```

## Stop the application

When the application was started with Docker Compose, stop the API and
PostgreSQL containers with:

```powershell
docker compose down
```

This keeps the `postgres-data` volume and its data. To stop only the API
container, use:

```powershell
docker compose stop api
```

When the API was started with `dotnet run`, press `Ctrl+C` in the terminal
running the API. If port `5000` remains occupied, free it with:

```powershell
Get-NetTCPConnection -LocalPort 5000 -State Listen |
	Select-Object -ExpandProperty OwningProcess -Unique |
	Stop-Process -Force
```

## Run the API locally

Development shortcut with Swagger:

```powershell
docker compose up -d postgres; dotnet run --project src\DocumentManagementSystem.Api\DocumentManagementSystem.Api.csproj --environment Development
```

Open Swagger at `http://localhost:5000/swagger`.

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

Make sure PostgreSQL is running first. You can start the Compose database
without starting the API:

```powershell
docker compose up -d postgres
```

The local API uses the connection string in
[appsettings.json](src/DocumentManagementSystem.Api/appsettings.json). Start
the API with:

```powershell
dotnet run --project src\DocumentManagementSystem.Api\DocumentManagementSystem.Api.csproj
```

Open Swagger at:

```text
http://localhost:5000/swagger
```

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