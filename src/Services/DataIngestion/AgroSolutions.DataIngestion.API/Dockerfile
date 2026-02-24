FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Services/DataIngestion/AgroSolutions.DataIngestion.API/AgroSolutions.DataIngestion.API.csproj", "src/Services/DataIngestion/AgroSolutions.DataIngestion.API/"]
COPY ["src/Services/DataIngestion/AgroSolutions.DataIngestion.Domain/AgroSolutions.DataIngestion.Domain.csproj", "src/Services/DataIngestion/AgroSolutions.DataIngestion.Domain/"]
COPY ["src/Services/DataIngestion/AgroSolutions.DataIngestion.Infrastructure/AgroSolutions.DataIngestion.Infrastructure.csproj", "src/Services/DataIngestion/AgroSolutions.DataIngestion.Infrastructure/"]
COPY ["src/BuildingBlocks/AgroSolutions.Common/AgroSolutions.Common.csproj", "src/BuildingBlocks/AgroSolutions.Common/"]
COPY ["src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/AgroSolutions.EventBus.RabbitMQ.csproj", "src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/"]

RUN dotnet restore "src/Services/DataIngestion/AgroSolutions.DataIngestion.API/AgroSolutions.DataIngestion.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Services/DataIngestion/AgroSolutions.DataIngestion.API"
RUN dotnet build "AgroSolutions.DataIngestion.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.DataIngestion.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.DataIngestion.API.dll"]
