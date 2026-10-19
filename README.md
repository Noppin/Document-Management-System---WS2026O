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

`dotnet test` runs the unit tests of every layer. They are split by project:
`tests/DocumentManagementSystem.Application.Tests`,
`tests/DocumentManagementSystem.Persistence.Tests`,
`tests/DocumentManagementSystem.Api.Tests` and
`tests/DocumentManagementSystem.OcrWorker.Tests` run against an in-memory
database, shared test fixtures live in `tests/DocumentManagementSystem.TestHelpers`.

## Start with Docker Compose

Docker Desktop must be running. From the repository root:

```powershell
docker compose up -d --build
```

`docker compose build` on its own is the required final check before a sprint is
handed in: every container image (`api`, `ocr-worker`, `frontend`) has to build
successfully.

This starts PostgreSQL, RabbitMQ, the REST API, the OCR worker, the React frontend
and Keycloak. Wait about a minute on the first start (Keycloak imports the realm),
then open:

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

## Sprint 3: OCR pipeline

Sprint 3 adds asynchronous OCR processing. The REST API stores the uploaded
document and publishes an `DocumentUploadedMessage` to RabbitMQ; a separate
.NET worker service consumes that message and will run the OCR later.
**No real OCR is performed in Sprint 3** — the worker deserializes the message,
logs the document and acknowledges it.

```text
POST /api/documents
        │
        ▼
DocumentService ── persist ──► PostgreSQL
        │ publish DocumentUploadedMessage
        ▼
RabbitMQ ── dms.document.ocr ──► OcrWorker (hosted service) ── log + ack
```

### Services

| Compose service | Image / build              | Purpose                                     |
| --------------- | -------------------------- | ------------------------------------------- |
| `postgres`      | `postgres:17-alpine`       | document metadata and file content          |
| `rabbitmq`      | `rabbitmq:4.1-management`  | OCR job queue with management UI            |
| `api`           | `src/DocumentManagementSystem.Api`     | REST API, publishes OCR jobs    |
| `ocr-worker`    | `src/DocumentManagementSystem.OcrWorker`| consumes OCR jobs             |
| `keycloak`      | `keycloak:26.0.7`          | OAuth2 / OIDC authorization server          |
| `frontend`      | `frontend/Dockerfile`      | nginx serving the React build               |

### Queue configuration

The publisher and the consumer read the same `RabbitMq` configuration section.

| Setting       | Environment variable (`docker-compose.yml`) | Default                |
| ------------- | ------------------------------------------- | ---------------------- |
| Host          | `RabbitMq__HostName`                        | `rabbitmq`             |
| Port          | `RabbitMq__Port`                            | `5672`                 |
| User          | `RabbitMq__UserName`                        | `dms`                  |
| Password      | `RabbitMq__Password`                        | `dms`                  |
| Queue name    | `RabbitMq__QueueName`                       | `dms.document.ocr`     |

The queue is durable, non-exclusive and not auto-declared as exclusive by the
publisher; both the API and the worker declare it with the same name, so the
order of container startup does not matter.

RabbitMQ management UI: <http://localhost:15672> (`dms` / `dms`).

### Layer-specific exception handling

| Layer       | Type                       | Behaviour                                                              |
| ----------- | -------------------------- | ---------------------------------------------------------------------- |
| Messaging   | `DocumentQueueException`   | wraps any publish error, already logged by `RabbitMqDocumentQueuePublisher` |
| Application | `DocumentService`          | deletes the persisted document when publishing fails, re-throws        |
| API         | `DocumentQueueExceptionHandler` | `503 Service Unavailable` with ProblemDetails, no RabbitMQ internals leaked |
| API         | `ValidationExceptionHandler` / `ArgumentExceptionHandler` | `400 Bad Request`                          |
| Worker      | `OcrMessageProcessingException` | reject without requeue (invalid payload)                            |
| Worker      | any other exception        | log and requeue the message                                             |

### Logging locations

Every log entry carries the document ID, so a document can be traced from the
REST upload through RabbitMQ into the worker:

| What                                  | Logger / file                          | Container   |
| ------------------------------------- | -------------------------------------- | ----------- |
| Upload persisted (document id, name)  | `DocumentService`                      | `api`       |
| Queue publish and publish failure     | `RabbitMqDocumentQueuePublisher`       | `api`       |
| Rollback after a failed publish       | `DocumentService` (warning)            | `api`       |
| Queue failure answered with 503       | `DocumentQueueExceptionHandler`        | `api`       |
| Worker startup and queue connection   | `Program.cs`, `OcrQueueWorker`         | `ocr-worker`|
| Received message (document id, name)  | `OcrMessageProcessor`                  | `ocr-worker`|
| Processing failures                   | `OcrQueueWorker`                       | `ocr-worker`|

```bash
docker compose logs api ocr-worker | grep <document-id>
```

### Sprint-3 smoke test

```bash
./scripts/ocr-smoke-test.sh
```

Uploads the bundled `http/sample.pdf` through the REST API, reads the created
document ID from the response and then proves the whole pipeline: the API
published the upload event and the OCR worker consumed the message, both
matched by the document ID in the container logs. Exits non-zero on failure.

See [docs/mid-term-review.md](docs/mid-term-review.md) for the mid-term review
notes (Publisher → RabbitMQ → Consumer flow, exception handling, log locations).

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
- `./scripts/ocr-smoke-test.sh` — upload the PDF and follow it through the OCR
  pipeline (see [Sprint-3 smoke test](#sprint-3-smoke-test)).

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
