# =============================================================================
# AgroSolutions - Script para Buildar Imagens Docker
# =============================================================================
# Este script builda todas as imagens Docker do projeto diretamente no
# ambiente Docker do minikube, evitando a necessidade de um registry externo.
#
# PRE-REQUISITOS:
# - Minikube em execucao (execute 02-start-minikube.ps1 primeiro)
#
# USO:
#   .\03-build-images.ps1
# =============================================================================

$ErrorActionPreference = "Stop"

# Diretorio raiz do projeto
$ProjectRoot = Split-Path -Parent $PSScriptRoot

function Write-Step {
    param([string]$message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$message)
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$message)
    Write-Host "[!] $message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$message)
    Write-Host "[ERRO] $message" -ForegroundColor Red
}

# =============================================================================
# INICIO DO SCRIPT
# =============================================================================

Write-Host @"

    _                    ____        _       _   _
   / \   __ _ _ __ ___  / ___|  ___ | |_   _| |_(_) ___  _ __  ___
  / _ \ / _` | '__/ _ \ \___ \ / _ \| | | | | __| |/ _ \| '_ \/ __|
 / ___ \ (_| | | | (_) | ___) | (_) | | |_| | |_| | (_) | | | \__ \
/_/   \_\__, |_|  \___/ |____/ \___/|_|\__,_|\__|_|\___/|_| |_|___/
        |___/

              Build de Imagens Docker

"@ -ForegroundColor Green

# =============================================================================
# 1. VERIFICAR MINIKUBE
# =============================================================================
Write-Step "1. Verificando minikube"

$ErrorActionPreference = "SilentlyContinue"
$minikubeStatus = minikube status --format='{{.Host}}' 2>$null
$ErrorActionPreference = "Stop"

if ($minikubeStatus -ne "Running") {
    Write-Error "Minikube nao esta em execucao! Execute 02-start-minikube.ps1 primeiro."
    Write-Host "Status atual: $minikubeStatus" -ForegroundColor Yellow
    exit 1
}
Write-Success "Minikube esta em execucao"

# =============================================================================
# 2. CONFIGURAR AMBIENTE DOCKER DO MINIKUBE
# =============================================================================
Write-Step "2. Configurando ambiente Docker do minikube"

Write-Host "Configurando variaveis de ambiente para usar Docker do minikube..." -ForegroundColor Yellow

# Obter e aplicar variaveis de ambiente do Docker do minikube
$ErrorActionPreference = "SilentlyContinue"
& minikube -p minikube docker-env --shell powershell | Invoke-Expression
$ErrorActionPreference = "Stop"

Write-Success "Ambiente Docker configurado para minikube"

# Verificar conexao com Docker do minikube
$ErrorActionPreference = "SilentlyContinue"
$dockerInfo = docker info 2>&1
$dockerExitCode = $LASTEXITCODE
$ErrorActionPreference = "Stop"

if ($dockerExitCode -ne 0) {
    Write-Error "Falha ao conectar com Docker do minikube!"
    exit 1
}
Write-Success "Conectado ao Docker do minikube"

# =============================================================================
# 3. BUILDAR IMAGENS
# =============================================================================
Write-Step "3. Buildando imagens Docker"

# Lista de imagens para buildar
$images = @(
    @{
        Name = "agrosolutions-identity-api"
        Dockerfile = "src/Services/Identity/AgroSolutions.Identity.API/Dockerfile"
        Context = "."
    },
    @{
        Name = "agrosolutions-property-api"
        Dockerfile = "src/Services/Property/AgroSolutions.Property.API/Dockerfile"
        Context = "."
    },
    @{
        Name = "agrosolutions-dataingestion-api"
        Dockerfile = "src/Services/DataIngestion/AgroSolutions.DataIngestion.API/Dockerfile"
        Context = "."
    },
    @{
        Name = "agrosolutions-alert-worker"
        Dockerfile = "src/Services/Alert/AgroSolutions.Alert.Worker/Dockerfile"
        Context = "."
    },
    @{
        Name = "agrosolutions-gateway"
        Dockerfile = "src/ApiGateway/AgroSolutions.Gateway/Dockerfile"
        Context = "."
    }
)

$buildSuccess = $true
$buildResults = @()

foreach ($image in $images) {
    Write-Host "`nBuildando: $($image.Name)" -ForegroundColor Yellow
    Write-Host "Dockerfile: $($image.Dockerfile)" -ForegroundColor Gray

    $dockerfilePath = Join-Path $ProjectRoot $image.Dockerfile
    $contextPath = Join-Path $ProjectRoot $image.Context

    if (-not (Test-Path $dockerfilePath)) {
        Write-Warning "Dockerfile nao encontrado: $dockerfilePath"
        $buildResults += @{Name = $image.Name; Status = "NAO ENCONTRADO"}
        continue
    }

    # Build da imagem
    $startTime = Get-Date
    docker build -t "$($image.Name):latest" -f $dockerfilePath $contextPath 2>&1 | ForEach-Object {
        if ($_ -match "error|Error|ERROR") {
            Write-Host $_ -ForegroundColor Red
        } elseif ($_ -match "Step|Successfully") {
            Write-Host $_ -ForegroundColor Gray
        }
    }

    $duration = (Get-Date) - $startTime

    if ($LASTEXITCODE -eq 0) {
        Write-Success "$($image.Name) buildado em $($duration.TotalSeconds.ToString('F1'))s"
        $buildResults += @{Name = $image.Name; Status = "OK"; Duration = $duration.TotalSeconds}
    } else {
        Write-Error "Falha ao buildar $($image.Name)"
        $buildResults += @{Name = $image.Name; Status = "FALHOU"}
        $buildSuccess = $false
    }
}

# =============================================================================
# 4. RESUMO DO BUILD
# =============================================================================
Write-Step "4. Resumo do Build"

Write-Host "`nResultado do build de imagens:" -ForegroundColor White
Write-Host "-" * 50

foreach ($result in $buildResults) {
    $statusColor = switch ($result.Status) {
        "OK" { "Green" }
        "FALHOU" { "Red" }
        default { "Yellow" }
    }

    $durationStr = if ($result.Duration) { " ($($result.Duration.ToString('F1'))s)" } else { "" }
    Write-Host "  $($result.Name): " -NoNewline
    Write-Host "$($result.Status)$durationStr" -ForegroundColor $statusColor
}

# =============================================================================
# 5. LISTAR IMAGENS DISPONIVEIS
# =============================================================================
Write-Step "5. Imagens disponiveis no minikube"

Write-Host "Imagens AgroSolutions:" -ForegroundColor White
docker images | Select-String "agrosolutions"

if ($buildSuccess) {
    Write-Host @"

BUILD CONCLUIDO COM SUCESSO!

As imagens foram buildadas diretamente no Docker do minikube.
Elas estao prontas para serem usadas nos deployments do Kubernetes.

PROXIMO PASSO:
  Execute: .\04-deploy-k8s.ps1

"@ -ForegroundColor Green
} else {
    Write-Host @"

ATENCAO: Algumas imagens falharam no build!
Verifique os erros acima e corrija antes de continuar.

"@ -ForegroundColor Yellow
}
