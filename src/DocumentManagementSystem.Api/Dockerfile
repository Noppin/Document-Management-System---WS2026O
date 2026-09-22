FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY DocumentManagementSystem.sln ./
COPY src/DocumentManagementSystem.Domain/DocumentManagementSystem.Domain.csproj src/DocumentManagementSystem.Domain/
COPY src/DocumentManagementSystem.Persistence/DocumentManagementSystem.Persistence.csproj src/DocumentManagementSystem.Persistence/
COPY src/DocumentManagementSystem.Api/DocumentManagementSystem.Api.csproj src/DocumentManagementSystem.Api/
RUN dotnet restore src/DocumentManagementSystem.Api/DocumentManagementSystem.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/DocumentManagementSystem.Api/DocumentManagementSystem.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.Api.dll"]
