FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/DocumentManagementSystem.Messaging/DocumentManagementSystem.Messaging.csproj src/DocumentManagementSystem.Messaging/
COPY src/DocumentManagementSystem.OcrWorker/DocumentManagementSystem.OcrWorker.csproj src/DocumentManagementSystem.OcrWorker/
RUN dotnet restore src/DocumentManagementSystem.OcrWorker/DocumentManagementSystem.OcrWorker.csproj

COPY src/ ./src/
RUN dotnet publish src/DocumentManagementSystem.OcrWorker/DocumentManagementSystem.OcrWorker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.OcrWorker.dll"]
