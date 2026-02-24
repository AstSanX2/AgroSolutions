# =============================================================================
# AgroSolutions - Script de Deploy no Kubernetes
# =============================================================================
# Este script faz o deploy de toda a infraestrutura e aplicacoes no minikube
# usando Kustomize.
#
# PRE-REQUISITOS:
# - Minikube em execucao (execute 02-start-minikube.ps1)
# - Imagens buildadas (execute 03-build-images.ps1)
#
# USO:
#   .\04-deploy-k8s.ps1
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

function Wait-ForPod {
    param(
        [string]$namespace,
        [string]$labelSelector,
        [int]$timeoutSeconds = 120
    )

    Write-Host "Aguardando pod com label '$labelSelector' ficar Ready..." -ForegroundColor Yellow

    $startTime = Get-Date
    while ($true) {
        $elapsed = ((Get-Date) - $startTime).TotalSeconds
        if ($elapsed -gt $timeoutSeconds) {
            Write-Warning "Timeout aguardando pod ficar Ready"
            return $false
        }

        $podStatus = kubectl get pods -n $namespace -l $labelSelector -o jsonpath="{.items[0].status.phase}" 2>$null
        $podReady = kubectl get pods -n $namespace -l $labelSelector -o jsonpath="{.items[0].status.conditions[?(@.type=='Ready')].status}" 2>$null

        if ($podStatus -eq "Running" -and $podReady -eq "True") {
            Write-Success "Pod esta Ready!"
            return $true
        }

        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
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

              Deploy no Kubernetes (Minikube)

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

# Verificar se as imagens existem
Write-Host "Verificando imagens Docker..." -ForegroundColor Yellow
$ErrorActionPreference = "SilentlyContinue"
& minikube -p minikube docker-env --shell powershell | Invoke-Expression
$ErrorActionPreference = "Stop"

$requiredImages = @(
    "agrosolutions-identity-api",
    "agrosolutions-property-api",
    "agrosolutions-dataingestion-api",
    "agrosolutions-alert-worker",
    "agrosolutions-gateway"
)

$missingImages = @()
foreach ($img in $requiredImages) {
    $exists = docker images -q "${img}:latest" 2>$null
    if (-not $exists) {
        $missingImages += $img
    }
}

if ($missingImages.Count -gt 0) {
    Write-Warning "Imagens nao encontradas: $($missingImages -join ', ')"
    Write-Host "Execute 03-build-images.ps1 para buildar as imagens" -ForegroundColor Yellow

    $continue = Read-Host "Deseja continuar mesmo assim? (s/n)"
    if ($continue -ne 's') {
        exit 1
    }
}

# =============================================================================
# 2. APLICAR MANIFESTS COM KUSTOMIZE
# =============================================================================
Write-Step "2. Aplicando manifests Kubernetes"

$kustomizePath = Join-Path $ProjectRoot "infra/k8s/overlays/dev"

if (-not (Test-Path $kustomizePath)) {
    Write-Error "Pasta Kustomize nao encontrada: $kustomizePath"
    exit 1
}

Write-Host "Usando Kustomize overlay: dev" -ForegroundColor Yellow
Write-Host "Caminho: $kustomizePath" -ForegroundColor Gray

# Aplicar com Kustomize
kubectl apply -k $kustomizePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha ao aplicar manifests!"
    exit 1
}

Write-Success "Manifests aplicados com sucesso!"

# =============================================================================
# 3. AGUARDAR PODS DE INFRAESTRUTURA
# =============================================================================
Write-Step "3. Aguardando infraestrutura ficar pronta"

$namespace = "agrosolutions"

Write-Host "`nAguardando MongoDB..." -ForegroundColor Yellow
Wait-ForPod -namespace $namespace -labelSelector "app=mongodb" -timeoutSeconds 180

Write-Host "`nAguardando RabbitMQ..." -ForegroundColor Yellow
Wait-ForPod -namespace $namespace -labelSelector "app=rabbitmq" -timeoutSeconds 180

# =============================================================================
# 4. STATUS DO DEPLOY
# =============================================================================
Write-Step "4. Status do Deploy"

Write-Host "`nNamespace: $namespace" -ForegroundColor White
Write-Host "-" * 50

Write-Host "`nPods:" -ForegroundColor White
kubectl get pods -n $namespace -o wide

Write-Host "`nServices:" -ForegroundColor White
kubectl get services -n $namespace

Write-Host "`nDeployments:" -ForegroundColor White
kubectl get deployments -n $namespace

Write-Host "`nStatefulSets:" -ForegroundColor White
kubectl get statefulsets -n $namespace

# =============================================================================
# 5. INFORMACOES DE ACESSO
# =============================================================================
Write-Step "5. Informacoes de Acesso"

$minikubeIP = minikube ip

Write-Host @"

INFRAESTRUTURA DEPLOYADA!

Para acessar os servicos, use um dos metodos abaixo:

METODO 1 - Port Forward (recomendado para desenvolvimento):
  # Gateway (porta 5000)
  kubectl port-forward -n agrosolutions svc/gateway 5000:80

  # MongoDB (porta 27017)
  kubectl port-forward -n agrosolutions svc/mongodb 27017:27017

  # RabbitMQ Management (porta 15672)
  kubectl port-forward -n agrosolutions svc/rabbitmq 15672:15672

METODO 2 - Minikube Service:
  # Abrir Gateway no navegador
  minikube service gateway -n agrosolutions

  # Abrir RabbitMQ Management
  minikube service rabbitmq -n agrosolutions --url

METODO 3 - Minikube Tunnel (LoadBalancer):
  # Em outro terminal, execute:
  minikube tunnel

  # Depois acesse via IP do LoadBalancer

CREDENCIAIS PADRAO:
  MongoDB:
    Host: localhost:27017 (com port-forward)
    Database: agrosolutions

  RabbitMQ:
    URL: http://localhost:15672 (com port-forward)
    Usuario: guest
    Senha: guest

COMANDOS UTEIS:
  kubectl logs -n agrosolutions -l app=<nome-do-app>
  kubectl describe pod -n agrosolutions <nome-do-pod>
  k9s -n agrosolutions

PROXIMO PASSO:
  Execute: .\05-test-infrastructure.ps1

"@ -ForegroundColor Green
