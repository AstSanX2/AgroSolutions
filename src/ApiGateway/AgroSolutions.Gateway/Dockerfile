FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/ApiGateway/AgroSolutions.Gateway/AgroSolutions.Gateway.csproj", "src/ApiGateway/AgroSolutions.Gateway/"]

RUN dotnet restore "src/ApiGateway/AgroSolutions.Gateway/AgroSolutions.Gateway.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/ApiGateway/AgroSolutions.Gateway"
RUN dotnet build "AgroSolutions.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.Gateway.dll"]
