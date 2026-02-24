FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["src/Services/Identity/AgroSolutions.Identity.API/AgroSolutions.Identity.API.csproj", "src/Services/Identity/AgroSolutions.Identity.API/"]
COPY ["src/Services/Identity/AgroSolutions.Identity.Domain/AgroSolutions.Identity.Domain.csproj", "src/Services/Identity/AgroSolutions.Identity.Domain/"]
COPY ["src/Services/Identity/AgroSolutions.Identity.Infrastructure/AgroSolutions.Identity.Infrastructure.csproj", "src/Services/Identity/AgroSolutions.Identity.Infrastructure/"]
COPY ["src/BuildingBlocks/AgroSolutions.Common/AgroSolutions.Common.csproj", "src/BuildingBlocks/AgroSolutions.Common/"]

RUN dotnet restore "src/Services/Identity/AgroSolutions.Identity.API/AgroSolutions.Identity.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/src/Services/Identity/AgroSolutions.Identity.API"
RUN dotnet build "AgroSolutions.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroSolutions.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "AgroSolutions.Identity.API.dll"]
