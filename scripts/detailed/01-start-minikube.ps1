# =============================================================================
# AgroSolutions - Script para Iniciar o Minikube
# =============================================================================
# Este script inicia o cluster minikube e configura o ambiente para desenvolvimento.
#
# PRE-REQUISITOS:
# - Docker Desktop em execucao
# - minikube instalado (execute 01-setup-environment.ps1 primeiro)
#
# USO:
#   .\02-start-minikube.ps1
# =============================================================================

$ErrorActionPreference = "Stop"

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

function Test-Command {
    param([string]$command)
    return $null -ne (Get-Command $command -ErrorAction SilentlyContinue)
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

              Inicializacao do Cluster Minikube

"@ -ForegroundColor Green

# =============================================================================
# 1. VERIFICAR PRE-REQUISITOS
# =============================================================================
Write-Step "1. Verificando pre-requisitos"

# Verificar Docker
if (-not (Test-Command "docker")) {
    Write-Error "Docker nao encontrado! Execute 01-setup-environment.ps1 primeiro."
    exit 1
}

$ErrorActionPreference = "SilentlyContinue"
$dockerInfo = docker info 2>&1
$dockerExitCode = $LASTEXITCODE
$ErrorActionPreference = "Stop"

if ($dockerExitCode -ne 0) {
    Write-Error "Docker nao esta em execucao! Inicie o Docker Desktop e tente novamente."
    exit 1
}
Write-Success "Docker esta em execucao"

# Verificar minikube
if (-not (Test-Command "minikube")) {
    Write-Error "minikube nao encontrado! Execute 01-setup-environment.ps1 primeiro."
    exit 1
}
Write-Success "minikube encontrado"

# Verificar kubectl
if (-not (Test-Command "kubectl")) {
    Write-Error "kubectl nao encontrado! Execute 01-setup-environment.ps1 primeiro."
    exit 1
}
Write-Success "kubectl encontrado"

# =============================================================================
# 2. VERIFICAR STATUS DO MINIKUBE
# =============================================================================
Write-Step "2. Verificando status do minikube"

$ErrorActionPreference = "SilentlyContinue"
$minikubeStatus = minikube status --format='{{.Host}}' 2>$null
$ErrorActionPreference = "Stop"

if ($minikubeStatus -eq "Running") {
    Write-Success "Minikube ja esta em execucao!"

    # Mostrar info do cluster
    Write-Host "`nInformacoes do cluster:" -ForegroundColor White
    minikube status
} else {
    Write-Warning "Minikube nao esta em execucao. Iniciando..."

    # =============================================================================
    # 3. INICIAR MINIKUBE
    # =============================================================================
    Write-Step "3. Iniciando minikube"

    Write-Host "Isso pode levar alguns minutos na primeira execucao..." -ForegroundColor Yellow

    # Iniciar minikube com driver Docker
    # Configuracoes:
    # - 2 CPUs e 4GB RAM (ajustado para funcionar com Docker Desktop padrao)
    # - Driver Docker (mais estavel no Windows)
    minikube start `
        --driver=docker `
        --cpus=2 `
        --memory=4096 `
        --disk-size=20g `
        --kubernetes-version=v1.28.0

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha ao iniciar o minikube!"
        Write-Host "Tente executar: minikube delete && minikube start --driver=docker" -ForegroundColor Yellow
        exit 1
    }

    Write-Success "Minikube iniciado com sucesso!"
}

# =============================================================================
# 4. CONFIGURAR KUBECTL
# =============================================================================
Write-Step "4. Configurando kubectl"

# Garantir que kubectl aponta para o minikube
kubectl config use-context minikube 2>&1 | Out-Null
Write-Success "kubectl configurado para usar o contexto minikube"

# =============================================================================
# 5. HABILITAR ADDONS NECESSARIOS
# =============================================================================
Write-Step "5. Habilitando addons do minikube"

$addons = @("ingress", "metrics-server", "dashboard")

foreach ($addon in $addons) {
    Write-Host "Habilitando addon: $addon" -ForegroundColor Yellow
    minikube addons enable $addon 2>&1 | Out-Null
    Write-Success "Addon $addon habilitado"
}

# =============================================================================
# 6. VERIFICAR CLUSTER
# =============================================================================
Write-Step "6. Verificando cluster"

Write-Host "Nodes do cluster:" -ForegroundColor White
kubectl get nodes

Write-Host "`nNamespaces:" -ForegroundColor White
kubectl get namespaces

# =============================================================================
# 7. CONFIGURAR DOCKER PARA USAR REGISTRY DO MINIKUBE
# =============================================================================
Write-Step "7. Informacoes do ambiente Docker do Minikube"

Write-Host @"

IMPORTANTE: Para buildar imagens diretamente no minikube, execute:
"@ -ForegroundColor Yellow

Write-Host @"

    # No PowerShell, execute:
    & minikube -p minikube docker-env --shell powershell | Invoke-Expression

    # Isso configura seu terminal para usar o Docker do minikube
    # Depois, suas imagens buildadas estarao disponiveis no cluster

"@ -ForegroundColor White

# =============================================================================
# RESUMO FINAL
# =============================================================================
Write-Step "MINIKUBE PRONTO!"

Write-Host @"

Cluster Kubernetes local esta pronto para uso!

COMANDOS UTEIS:
  minikube status          - Ver status do cluster
  minikube dashboard       - Abrir dashboard web do Kubernetes
  minikube stop           - Parar o cluster (preserva dados)
  minikube delete         - Deletar o cluster completamente
  k9s                     - Abrir visualizador de cluster no terminal

PROXIMOS PASSOS:
  1. Execute: .\03-build-images.ps1    - Buildar imagens Docker
  2. Execute: .\04-deploy-k8s.ps1      - Deploy no Kubernetes
  3. Execute: .\05-test-infrastructure.ps1 - Testar infraestrutura

"@ -ForegroundColor Green

# Mostrar IP do minikube
$minikubeIP = minikube ip
Write-Host "IP do Minikube: $minikubeIP" -ForegroundColor Cyan
Write-Host "`n"
