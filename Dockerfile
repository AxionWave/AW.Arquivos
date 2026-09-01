# AW.Arquivos API (.NET 8)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Arquivos.sln Directory.Build.props ./
COPY src/Arquivos.Core/Arquivos.Core.csproj src/Arquivos.Core/
COPY src/Arquivos.Application/Arquivos.Application.csproj src/Arquivos.Application/
COPY src/Arquivos.Infrastructure/Arquivos.Infrastructure.csproj src/Arquivos.Infrastructure/
COPY src/Arquivos.API/Arquivos.API.csproj src/Arquivos.API/
RUN dotnet restore
COPY src ./src
RUN dotnet publish src/Arquivos.API/Arquivos.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "Arquivos.API.dll"]
