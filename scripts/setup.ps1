# =============================================================================
# AgroSolutions - Script de Instalacao do Ambiente Local
# =============================================================================
# Este script instala todas as dependencias necessarias para rodar o projeto
# no Kubernetes local usando minikube.
#
# PRE-REQUISITOS:
# - Windows 10/11 com Hyper-V habilitado OU Docker Desktop instalado
# - PowerShell executando como Administrador
# - Winget (Windows Package Manager) instalado
#
# USO:
#   .\01-setup-environment.ps1
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

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-Command {
    param([string]$command)
    return $null -ne (Get-Command $command -ErrorAction SilentlyContinue)
}

function Install-WithWinget {
    param(
        [string]$packageId,
        [string]$packageName
    )

    Write-Host "Instalando $packageName..." -ForegroundColor Yellow

    try {
        winget install --id $packageId --accept-source-agreements --accept-package-agreements --silent
        if ($LASTEXITCODE -eq 0) {
            Write-Success "$packageName instalado com sucesso!"
            return $true
        }
    }
    catch {
        Write-Warning "Falha ao instalar $packageName via winget"
    }
    return $false
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

         Setup do Ambiente de Desenvolvimento Local

"@ -ForegroundColor Green

# Verificar se esta rodando como admin
if (-not (Test-Administrator)) {
    Write-Error "Este script precisa ser executado como Administrador!"
    Write-Host "Clique com botao direito no PowerShell e selecione 'Executar como Administrador'" -ForegroundColor Yellow
    exit 1
}

Write-Success "Executando como Administrador"

# Verificar winget
if (-not (Test-Command "winget")) {
    Write-Error "Winget nao encontrado! Instale o App Installer da Microsoft Store."
    exit 1
}
Write-Success "Winget encontrado"

# =============================================================================
# 1. VERIFICAR/INSTALAR DOCKER DESKTOP
# =============================================================================
Write-Step "1. Verificando Docker Desktop"

if (Test-Command "docker") {
    $dockerVersion = docker --version
    Write-Success "Docker encontrado: $dockerVersion"

    # Verificar se Docker esta rodando (ignorar warnings)
    $ErrorActionPreference = "SilentlyContinue"
    $dockerInfo = docker info 2>&1
    $dockerExitCode = $LASTEXITCODE
    $ErrorActionPreference = "Stop"

    if ($dockerExitCode -eq 0) {
        Write-Success "Docker esta em execucao"
    } else {
        Write-Warning "Docker instalado mas nao esta em execucao. Inicie o Docker Desktop."
    }
} else {
    Write-Warning "Docker Desktop nao encontrado. Instalando..."
    Install-WithWinget "Docker.DockerDesktop" "Docker Desktop"
    Write-Host "`nAPOS A INSTALACAO:" -ForegroundColor Yellow
    Write-Host "1. Reinicie o computador" -ForegroundColor Yellow
    Write-Host "2. Abra o Docker Desktop e aguarde inicializar" -ForegroundColor Yellow
    Write-Host "3. Execute este script novamente" -ForegroundColor Yellow
    exit 0
}

# =============================================================================
# 2. INSTALAR KUBECTL
# =============================================================================
Write-Step "2. Verificando kubectl"

if (Test-Command "kubectl") {
    $ErrorActionPreference = "SilentlyContinue"
    $kubectlVersion = (kubectl version --client -o json 2>$null | ConvertFrom-Json).clientVersion.gitVersion
    $ErrorActionPreference = "Stop"
    if (-not $kubectlVersion) { $kubectlVersion = "versao desconhecida" }
    Write-Success "kubectl encontrado: $kubectlVersion"
} else {
    Write-Warning "kubectl nao encontrado. Instalando..."
    Install-WithWinget "Kubernetes.kubectl" "kubectl"

    # Adicionar ao PATH se necessario
    $kubectlPath = "$env:LOCALAPPDATA\Microsoft\WinGet\Links"
    if ($env:PATH -notlike "*$kubectlPath*") {
        [Environment]::SetEnvironmentVariable("PATH", "$env:PATH;$kubectlPath", [EnvironmentVariableTarget]::User)
        $env:PATH = "$env:PATH;$kubectlPath"
    }
}

# =============================================================================
# 3. INSTALAR MINIKUBE
# =============================================================================
Write-Step "3. Verificando minikube"

if (Test-Command "minikube") {
    $ErrorActionPreference = "SilentlyContinue"
    $minikubeVersion = (minikube version 2>$null | Select-String "minikube version") -replace "minikube version: ", ""
    $ErrorActionPreference = "Stop"
    if (-not $minikubeVersion) { $minikubeVersion = "instalado" }
    Write-Success "minikube encontrado: $minikubeVersion"
} else {
    Write-Warning "minikube nao encontrado. Instalando..."
    Install-WithWinget "Kubernetes.minikube" "minikube"
}

# =============================================================================
# 4. INSTALAR K9S (OPCIONAL - VISUALIZADOR DE CLUSTER)
# =============================================================================
Write-Step "4. Verificando k9s (visualizador de cluster)"

if (Test-Command "k9s") {
    Write-Success "k9s encontrado"
} else {
    Write-Warning "k9s nao encontrado. Instalando..."
    Install-WithWinget "Derailed.k9s" "k9s"
}

# =============================================================================
# 5. INSTALAR HELM (GERENCIADOR DE PACOTES K8S)
# =============================================================================
Write-Step "5. Verificando Helm"

if (Test-Command "helm") {
    Write-Success "Helm encontrado"
} else {
    Write-Warning "Helm nao encontrado. Instalando..."
    Install-WithWinget "Helm.Helm" "Helm"
}

# =============================================================================
# RESUMO FINAL
# =============================================================================
Write-Step "RESUMO DA INSTALACAO"

Write-Host "`nFerramentas instaladas:" -ForegroundColor White

$tools = @(
    @{Name="Docker"; Command="docker"},
    @{Name="kubectl"; Command="kubectl"},
    @{Name="minikube"; Command="minikube"},
    @{Name="k9s"; Command="k9s"},
    @{Name="Helm"; Command="helm"}
)

$allInstalled = $true
$ErrorActionPreference = "SilentlyContinue"
foreach ($tool in $tools) {
    if (Test-Command $tool.Command) {
        Write-Host "  [x] $($tool.Name)" -ForegroundColor Green
    } else {
        Write-Host "  [ ] $($tool.Name) - NAO INSTALADO" -ForegroundColor Red
        $allInstalled = $false
    }
}
$ErrorActionPreference = "Stop"

if ($allInstalled) {
    Write-Host "`n" -NoNewline
    Write-Success "Todas as ferramentas foram instaladas com sucesso!"
    Write-Host "`nPROXIMOS PASSOS:" -ForegroundColor Cyan
    Write-Host "1. Feche e reabra o PowerShell para atualizar o PATH" -ForegroundColor White
    Write-Host "2. Certifique-se de que o Docker Desktop esta em execucao" -ForegroundColor White
    Write-Host "3. Execute: .\02-start-minikube.ps1" -ForegroundColor White
} else {
    Write-Host "`n" -NoNewline
    Write-Warning "Algumas ferramentas nao foram instaladas."
    Write-Host "Tente instalar manualmente ou execute o script novamente." -ForegroundColor Yellow
}

Write-Host "`n"
