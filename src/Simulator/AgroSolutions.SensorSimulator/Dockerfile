FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/Simulator/AgroSolutions.SensorSimulator/AgroSolutions.SensorSimulator.csproj", "src/Simulator/AgroSolutions.SensorSimulator/"]

RUN dotnet restore "src/Simulator/AgroSolutions.SensorSimulator/AgroSolutions.SensorSimulator.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Simulator/AgroSolutions.SensorSimulator"
RUN dotnet build "AgroSolutions.SensorSimulator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.SensorSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.SensorSimulator.dll"]
