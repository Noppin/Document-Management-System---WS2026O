FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/DocumentManagementSystem.Messaging/DocumentManagementSystem.Messaging.csproj src/DocumentManagementSystem.Messaging/
COPY src/DocumentManagementSystem.Search/DocumentManagementSystem.Search.csproj src/DocumentManagementSystem.Search/
COPY src/DocumentManagementSystem.IndexingWorker/DocumentManagementSystem.IndexingWorker.csproj src/DocumentManagementSystem.IndexingWorker/
RUN dotnet restore src/DocumentManagementSystem.IndexingWorker/DocumentManagementSystem.IndexingWorker.csproj

COPY src/ ./src/
RUN dotnet publish src/DocumentManagementSystem.IndexingWorker/DocumentManagementSystem.IndexingWorker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.IndexingWorker.dll"]
