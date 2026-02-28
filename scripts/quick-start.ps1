# =============================================================================
# AgroSolutions - Quick Start
# =============================================================================
# Este script executa todos os passos necessarios para subir o ambiente
# completo. Ele verifica o que ja foi feito e continua de onde parou.
#
# PRE-REQUISITOS:
# - Docker Desktop instalado e em execucao
# - minikube instalado (ou execute 01-setup-environment.ps1 primeiro)
#
# USO:
#   .\quick-start.ps1
# =============================================================================

$ErrorActionPreference = "SilentlyContinue"
$ScriptDir = $PSScriptRoot
$ProjectRoot = Split-Path -Parent $ScriptDir

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

function Write-Skip {
    param([string]$message)
    Write-Host "[SKIP] $message" -ForegroundColor DarkGray
}

function Test-MinikubeRunning {
    $status = minikube status --format='{{.Host}}' 2>$null
    return $status -eq "Running"
}

function Test-ImagesBuilt {
    # Configurar Docker do minikube
    & minikube -p minikube docker-env --shell powershell 2>$null | Invoke-Expression

    $requiredImages = @(
        "agrosolutions-identity-api",
        "agrosolutions-property-api",
        "agrosolutions-dataingestion-api",
        "agrosolutions-alert-worker",
        "agrosolutions-gateway",
        "agrosolutions-sensor-simulator"
    )

    foreach ($img in $requiredImages) {
        $exists = docker images -q "${img}:latest" 2>$null
        if (-not $exists) {
            return $false
        }
    }
    return $true
}

function Test-K8sDeployed {
    $namespace = kubectl get namespace agrosolutions -o jsonpath="{.metadata.name}" 2>$null
    if ($namespace -ne "agrosolutions") {
        return $false
    }

    $pods = kubectl get pods -n agrosolutions --no-headers 2>$null
    if (-not $pods) {
        return $false
    }
    return $true
}

Write-Host @"

    _                    ____        _       _   _
   / \   __ _ _ __ ___  / ___|  ___ | |_   _| |_(_) ___  _ __  ___
  / _ \ / _` | '__/ _ \ \___ \ / _ \| | | | | __| |/ _ \| '_ \/ __|
 / ___ \ (_| | | | (_) | ___) | (_) | | |_| | |_| | (_) | | | \__ \
/_/   \_\__, |_|  \___/ |____/ \___/|_|\__,_|\__|_|\___/|_| |_|___/
        |___/

              Quick Start - Deploy Completo

"@ -ForegroundColor Green

# =============================================================================
# VERIFICAR PRE-REQUISITOS
# =============================================================================
Write-Step "Verificando pre-requisitos"

# Verificar Docker
$dockerInfo = docker info 2>&1
$dockerExitCode = $LASTEXITCODE

if ($dockerExitCode -ne 0) {
    Write-Error "Docker nao esta em execucao!"
    Write-Host "Inicie o Docker Desktop e tente novamente." -ForegroundColor Yellow
    exit 1
}
Write-Success "Docker esta em execucao"

# Verificar minikube
if (-not (Get-Command minikube -ErrorAction SilentlyContinue)) {
    Write-Error "minikube nao encontrado!"
    Write-Host "Execute primeiro: .\01-setup-environment.ps1" -ForegroundColor Yellow
    exit 1
}
Write-Success "minikube encontrado"

# =============================================================================
# PASSO 1: MINIKUBE
# =============================================================================
Write-Step "PASSO 1/4: Minikube"

if (Test-MinikubeRunning) {
    Write-Skip "Minikube ja esta em execucao - pulando"
} else {
    Write-Host "Iniciando minikube..." -ForegroundColor Yellow

    minikube start `
        --driver=docker `
        --cpus=2 `
        --memory=4096 `
        --disk-size=20g `
        --kubernetes-version=v1.28.0

    if (-not (Test-MinikubeRunning)) {
        Write-Error "Falha ao iniciar minikube!"
        exit 1
    }
    Write-Success "Minikube iniciado"
}

# Aguardar API server ficar pronto antes de habilitar addons
Write-Host "Aguardando Kubernetes API server ficar pronto..." -ForegroundColor Gray
$retries = 0
while ($retries -lt 15) {
    $ready = kubectl get nodes 2>$null
    if ($LASTEXITCODE -eq 0) { break }
    Start-Sleep -Seconds 5
    $retries++
}

# Habilitar addons
Write-Host "Habilitando addons..." -ForegroundColor Gray
minikube addons enable ingress 2>$null | Out-Null
minikube addons enable metrics-server 2>$null | Out-Null

# =============================================================================
# PASSO 2: BUILD IMAGENS
# =============================================================================
Write-Step "PASSO 2/4: Build de Imagens Docker"

# Configurar Docker do minikube
Write-Host "Configurando ambiente Docker do minikube..." -ForegroundColor Gray
& minikube -p minikube docker-env --shell powershell 2>$null | Invoke-Expression

if (Test-ImagesBuilt) {
    Write-Skip "Todas as imagens ja foram buildadas - pulando"
} else {
    Write-Host "Buildando imagens..." -ForegroundColor Yellow

    $images = @(
        @{Name = "agrosolutions-identity-api"; Dockerfile = "src/Services/Identity/AgroSolutions.Identity.API/Dockerfile"},
        @{Name = "agrosolutions-property-api"; Dockerfile = "src/Services/Property/AgroSolutions.Property.API/Dockerfile"},
        @{Name = "agrosolutions-dataingestion-api"; Dockerfile = "src/Services/DataIngestion/AgroSolutions.DataIngestion.API/Dockerfile"},
        @{Name = "agrosolutions-alert-worker"; Dockerfile = "src/Services/Alert/AgroSolutions.Alert.Worker/Dockerfile"},
        @{Name = "agrosolutions-gateway"; Dockerfile = "src/ApiGateway/AgroSolutions.Gateway/Dockerfile"},
        @{Name = "agrosolutions-sensor-simulator"; Dockerfile = "src/Simulator/AgroSolutions.SensorSimulator/Dockerfile"}
    )

    $buildFailed = $false
    foreach ($image in $images) {
        $dockerfilePath = Join-Path $ProjectRoot $image.Dockerfile

        if (-not (Test-Path $dockerfilePath)) {
            Write-Warning "Dockerfile nao encontrado: $($image.Dockerfile)"
            continue
        }

        Write-Host "  Buildando $($image.Name)..." -NoNewline

        docker build -t "$($image.Name):latest" -f $dockerfilePath $ProjectRoot 2>&1 | Out-Null

        if ($LASTEXITCODE -eq 0) {
            Write-Host " OK" -ForegroundColor Green
        } else {
            Write-Host " FALHOU" -ForegroundColor Red
            $buildFailed = $true
        }
    }

    if ($buildFailed) {
        Write-Error "Algumas imagens falharam no build!"
        exit 1
    }
    Write-Success "Todas as imagens buildadas"
}

# =============================================================================
# PASSO 3: DEPLOY K8S
# =============================================================================
Write-Step "PASSO 3/4: Deploy no Kubernetes"

if (Test-K8sDeployed) {
    Write-Skip "Deploy ja realizado - pulando"
    Write-Host "Para forcar redeploy, execute: kubectl delete namespace agrosolutions" -ForegroundColor Gray
} else {
    Write-Host "Aplicando manifests Kubernetes..." -ForegroundColor Yellow

    $kustomizePath = Join-Path $ProjectRoot "infra/k8s/overlays/dev"

    kubectl apply -k $kustomizePath 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao aplicar manifests!"
        exit 1
    }

    Write-Success "Manifests aplicados"

    # Aguardar pods iniciarem
    Write-Host "Aguardando pods iniciarem (60s)..." -ForegroundColor Yellow
    Start-Sleep -Seconds 60
}

# =============================================================================
# PASSO 4: TESTE
# =============================================================================
Write-Step "PASSO 4/4: Verificando Infraestrutura"

$namespace = "agrosolutions"

Write-Host "`nStatus dos Pods:" -ForegroundColor White
kubectl get pods -n $namespace

$allRunning = $true
$pods = kubectl get pods -n $namespace -o jsonpath="{.items[*].status.phase}" 2>$null
if ($pods) {
    $podList = $pods -split " "
    foreach ($status in $podList) {
        if ($status -ne "Running" -and $status -ne "Succeeded") {
            $allRunning = $false
        }
    }
}

Write-Host ""
if ($allRunning -and $pods) {
    Write-Success "Todos os pods estao Running!"
} else {
    Write-Warning "Alguns pods ainda estao inicializando. Aguarde mais alguns minutos."
    Write-Host "Execute 'kubectl get pods -n agrosolutions' para verificar o status." -ForegroundColor Gray
}

# =============================================================================
# RESUMO FINAL
# =============================================================================
Write-Host @"

========================================
         QUICK START CONCLUIDO!
========================================

"@ -ForegroundColor Green

Write-Host "URLS DE ACESSO (com port-forward ativo):" -ForegroundColor Cyan
Write-Host "  Gateway:        http://localhost:5000/health" -ForegroundColor Gray
Write-Host "  Auth API:       http://localhost:5000/api/auth/me" -ForegroundColor Gray
Write-Host "  Property API:   http://localhost:5000/api/properties" -ForegroundColor Gray
Write-Host "  Sensors API:    http://localhost:5000/api/sensors" -ForegroundColor Gray
Write-Host "  Grafana:        http://localhost:3000 (admin/admin)" -ForegroundColor Gray
Write-Host "  RabbitMQ:       http://localhost:15672 (guest/guest)" -ForegroundColor Gray
Write-Host ""

Write-Host "Para iniciar o port-forward:" -ForegroundColor White
Write-Host "  Gateway (API):  kubectl port-forward -n agrosolutions svc/gateway 5000:80" -ForegroundColor Yellow
Write-Host "  Grafana:        kubectl port-forward -n agrosolutions svc/grafana 3000:3000" -ForegroundColor Yellow
Write-Host "  RabbitMQ:       kubectl port-forward -n agrosolutions svc/rabbitmq 15672:15672" -ForegroundColor Yellow
Write-Host ""

$portForward = Read-Host "Deseja iniciar o port-forward do Gateway e Grafana agora? (s/n)"
if ($portForward -eq 's') {
    Write-Host "`nIniciando port-forward..." -ForegroundColor Yellow
    Write-Host "  Gateway: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "  Grafana: http://localhost:3000 (admin/admin)" -ForegroundColor Cyan
    Write-Host "Pressione Ctrl+C para encerrar`n" -ForegroundColor Gray

    # Iniciar Grafana port-forward em background
    Start-Job -ScriptBlock { kubectl port-forward -n agrosolutions svc/grafana 3000:3000 } | Out-Null
    # Gateway em foreground (bloqueia ate Ctrl+C)
    kubectl port-forward -n agrosolutions svc/gateway 5000:80
} else {
    Write-Host "Outras opcoes:" -ForegroundColor White
    Write-Host "  .\scripts\manage.ps1       - Gerenciar servicos" -ForegroundColor Yellow
    Write-Host "  .\scripts\test.ps1         - Verificar status" -ForegroundColor Yellow
    Write-Host "  .\scripts\stop.ps1         - Parar tudo" -ForegroundColor Yellow
    Write-Host ""
}
