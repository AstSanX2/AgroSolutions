FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Services/Alert/AgroSolutions.Alert.Worker/AgroSolutions.Alert.Worker.csproj", "src/Services/Alert/AgroSolutions.Alert.Worker/"]
COPY ["src/Services/Alert/AgroSolutions.Alert.Domain/AgroSolutions.Alert.Domain.csproj", "src/Services/Alert/AgroSolutions.Alert.Domain/"]
COPY ["src/Services/Alert/AgroSolutions.Alert.Infrastructure/AgroSolutions.Alert.Infrastructure.csproj", "src/Services/Alert/AgroSolutions.Alert.Infrastructure/"]
COPY ["src/BuildingBlocks/AgroSolutions.Common/AgroSolutions.Common.csproj", "src/BuildingBlocks/AgroSolutions.Common/"]
COPY ["src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/AgroSolutions.EventBus.RabbitMQ.csproj", "src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/"]

RUN dotnet restore "src/Services/Alert/AgroSolutions.Alert.Worker/AgroSolutions.Alert.Worker.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Services/Alert/AgroSolutions.Alert.Worker"
RUN dotnet build "AgroSolutions.Alert.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.Alert.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.Alert.Worker.dll"]
