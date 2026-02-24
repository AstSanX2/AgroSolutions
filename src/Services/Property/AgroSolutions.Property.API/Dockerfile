FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Services/Property/AgroSolutions.Property.API/AgroSolutions.Property.API.csproj", "src/Services/Property/AgroSolutions.Property.API/"]
COPY ["src/Services/Property/AgroSolutions.Property.Domain/AgroSolutions.Property.Domain.csproj", "src/Services/Property/AgroSolutions.Property.Domain/"]
COPY ["src/Services/Property/AgroSolutions.Property.Infrastructure/AgroSolutions.Property.Infrastructure.csproj", "src/Services/Property/AgroSolutions.Property.Infrastructure/"]
COPY ["src/BuildingBlocks/AgroSolutions.Common/AgroSolutions.Common.csproj", "src/BuildingBlocks/AgroSolutions.Common/"]
COPY ["src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/AgroSolutions.EventBus.RabbitMQ.csproj", "src/BuildingBlocks/AgroSolutions.EventBus.RabbitMQ/"]

RUN dotnet restore "src/Services/Property/AgroSolutions.Property.API/AgroSolutions.Property.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Services/Property/AgroSolutions.Property.API"
RUN dotnet build "AgroSolutions.Property.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.Property.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.Property.API.dll"]
