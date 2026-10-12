# Document Management System

React frontend + ASP.NET Core Web API for document uploads, with PostgreSQL for
metadata and Keycloak for authentication.

## Project management

Tickets and task tracking are managed in Jira:
<https://nipponlama.atlassian.net/jira/software/projects/DMS/boards/1>

## Requirements

- .NET 10 SDK
- Docker (Docker Desktop running)

## Restore and build

From the repository root:

```powershell
dotnet restore DocumentManagementSystem.sln
dotnet build DocumentManagementSystem.sln
dotnet test DocumentManagementSystem.sln
```

`dotnet test` runs the unit tests in
`tests/DocumentManagementSystem.Persistence.Tests` against an in-memory database.

## Start with Docker Compose

Docker Desktop must be running. From the repository root:

```powershell
docker compose up -d --build
```

This starts PostgreSQL, the REST API, the React frontend and Keycloak. Wait about
a minute on the first start (Keycloak imports the realm), then open:

```text
http://localhost/
```

nginx serves the frontend and proxies `/api/*` to the API container, so no host or
port has to be configured on the client. The `api` and `postgres` containers are
not published to the host; the local `docker-compose.override.yml` maps the API to
`http://localhost:8080` and PostgreSQL to `localhost:5433` for debugging.

Check the running services:

```powershell
docker compose ps
Invoke-WebRequest http://localhost/api/health -UseBasicParsing
```

The expected health response is `200 OK` with `{"status":"ok"}`.

The PostgreSQL container keeps the `postgres-data` volume, so uploaded documents
remain available after a restart.

Stop the containers (keeps the database volume):

```powershell
docker compose down
```

Remove the database volume as well:

```powershell
docker compose down -v
```

## Login (Keycloak)

The REST API is protected with OAuth2 / OpenID Connect; authentication is
delegated to the Keycloak service, so the user database lives in Keycloak. The
frontend logs the user in (`login-required`), stores the access token and sends it
as a `Bearer` token to the API, which validates it against the `dms` realm and the
`api` audience. The `documents` and `collections` endpoints return `401` without a
token; `GET /health` is public.

Test users:

| User    | Password   | Role        |
| ------- | ---------- | ----------- |
| `alice` | `password` | user        |
| `admin` | `admin`    | admin, user |

Keycloak admin console: <http://localhost:8180> (`admin` / `admin`). The realm and
test users are versioned in
[keycloak/realm-export.json](keycloak/realm-export.json).

### Call the API with a token

```bash
TOKEN=$(curl -s -X POST \
  http://localhost:8180/realms/dms/protocol/openid-connect/token \
  -d grant_type=password -d client_id=frontend \
  -d username=alice -d password=password | jq -r .access_token)

curl -H "Authorization: Bearer $TOKEN" http://localhost/api/documents
```

## Frontend

nginx serves the production build on port `80`. The Vite development server is not
required in production.

```bash
cd frontend
npm ci
npm run dev     # dev server on http://localhost:5173, proxies /api to :8080
npm run build   # type-check and production build into dist
npm test        # run the test suite once
```

## Run the API locally (Swagger)

```powershell
docker compose up -d postgres keycloak
dotnet run --project src\DocumentManagementSystem.Api\DocumentManagementSystem.Api.csproj --environment Development
```

Swagger: <http://localhost:5000/swagger>.

## Endpoints

`documents`/`collections` require a Keycloak access token; `GET /health` is public.
The API applies pending EF Core migrations automatically on startup
(`Database.Migrate()`), so the PostgreSQL schema is created and kept up to date.

To try the API in Swagger, click **Authorize**, paste a Keycloak access token (see
[Call the API with a token](#call-the-api-with-a-token)) and use the endpoints.
Uploads must be non-empty PDFs with the `application/pdf` content type and a
`%PDF-` header.

## Manual API testing

- [http/paperless.http](http/paperless.http) — REST Client requests; the first
  request fetches a Keycloak token and the rest reuse it.
- `./scripts/upload-document.ps1` / `./scripts/upload-document.sh` — upload the
  bundled `http/sample.pdf` (defaults to `http://localhost/api`).

## Smoke test

```bash
./scripts/smoke-test.sh
```

Verifies the Compose setup end to end: services running, nginx serves the build,
SPA routes, protected endpoints answer `401` without and `200` with a token,
Keycloak serves the `dms` realm and PostgreSQL accepts connections. Exits non-zero
on failure.

## CI & Releases

GitHub Actions in [.github/workflows](.github/workflows):

- `ci.yml` builds and tests backend and frontend on pushes/PRs to `dev` and `main`.
- `sprint-release.yml` creates a release when a `sprint-<n>` tag is pushed. The
  tagged commit must be on `main`; GitHub then attaches the source zip.
