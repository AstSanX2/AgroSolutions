# =============================================================================
# AgroSolutions - Gerenciamento de Servicos
# =============================================================================
# Script central para gerenciar servicos: redeploy, logs, port-forward.
#
# USO:
#   .\manage.ps1                                      # Menu interativo
#   .\manage.ps1 -All                                 # Redeploy de tudo
#   .\manage.ps1 -Service gateway                     # Redeploy de um servico
#   .\manage.ps1 -List                                # Listar servicos
#   .\manage.ps1 -LogLevel Debug                      # Log Debug em TODOS
#   .\manage.ps1 -LogLevel Debug -Service gateway     # Log Debug no gateway
#   .\manage.ps1 -LogLevel Information                # Voltar ao padrao
#   .\manage.ps1 -PortForward                         # Iniciar port-forward
#
# LOG LEVELS: Trace, Debug, Information, Warning, Error, Critical
#
# SERVICOS DISPONIVEIS:
#   gateway, identity, property, dataingestion, alert
# =============================================================================

param(
    [switch]$All,
    [switch]$List,
    [switch]$PortForward,
    [string]$Service,
    [string]$LogLevel
)

$ErrorActionPreference = "SilentlyContinue"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$namespace = "agrosolutions"

# Mapeamento de serviços
$services = @{
    "gateway" = @{
        Image = "agrosolutions-gateway"
        Dockerfile = "src/ApiGateway/AgroSolutions.Gateway/Dockerfile"
        K8sName = "gateway"
        Type = "deployment"
    }
    "identity" = @{
        Image = "agrosolutions-identity-api"
        Dockerfile = "src/Services/Identity/AgroSolutions.Identity.API/Dockerfile"
        K8sName = "identity-api"
        Type = "deployment"
    }
    "property" = @{
        Image = "agrosolutions-property-api"
        Dockerfile = "src/Services/Property/AgroSolutions.Property.API/Dockerfile"
        K8sName = "property-api"
        Type = "deployment"
    }
    "dataingestion" = @{
        Image = "agrosolutions-dataingestion-api"
        Dockerfile = "src/Services/DataIngestion/AgroSolutions.DataIngestion.API/Dockerfile"
        K8sName = "dataingestion-api"
        Type = "deployment"
    }
    "alert" = @{
        Image = "agrosolutions-alert-worker"
        Dockerfile = "src/Services/Alert/AgroSolutions.Alert.Worker/Dockerfile"
        K8sName = "alert-worker"
        Type = "deployment"
    }
}

function Write-Title {
    Write-Host @"

    _                    ____        _       _   _
   / \   __ _ _ __ ___  / ___|  ___ | |_   _| |_(_) ___  _ __  ___
  / _ \ / _` | '__/ _ \ \___ \ / _ \| | | | | __| |/ _ \| '_ \/ __|
 / ___ \ (_| | | | (_) | ___) | (_) | | |_| | |_| | (_) | | | \__ \
/_/   \_\__, |_|  \___/ |____/ \___/|_|\__,_|\__|_|\___/|_| |_|___/
        |___/

               Gerenciamento de Servicos

"@ -ForegroundColor Green
}

function Write-Success {
    param([string]$message)
    Write-Host "[OK] $message" -ForegroundColor Green
}

function Write-Info {
    param([string]$message)
    Write-Host "[*] $message" -ForegroundColor Cyan
}

function Write-Error {
    param([string]$message)
    Write-Host "[ERRO] $message" -ForegroundColor Red
}

function Show-ServiceList {
    Write-Host "`nServicos disponiveis:" -ForegroundColor White
    Write-Host "-" * 40
    foreach ($key in $services.Keys | Sort-Object) {
        $svc = $services[$key]
        Write-Host "  $key" -ForegroundColor Yellow -NoNewline
        Write-Host " -> $($svc.Image)" -ForegroundColor Gray
    }
    Write-Host ""
}

function Build-Service {
    param([string]$serviceName)

    $svc = $services[$serviceName]
    if (-not $svc) {
        Write-Error "Servico '$serviceName' nao encontrado"
        return $false
    }

    Write-Info "Buildando $($svc.Image)..."

    $dockerfilePath = Join-Path $ProjectRoot $svc.Dockerfile

    if (-not (Test-Path $dockerfilePath)) {
        Write-Error "Dockerfile nao encontrado: $($svc.Dockerfile)"
        return $false
    }

    docker build -t "$($svc.Image):latest" -f $dockerfilePath $ProjectRoot 2>&1 | Out-Null

    if ($LASTEXITCODE -eq 0) {
        Write-Success "$($svc.Image) buildado"
        return $true
    } else {
        Write-Error "Falha ao buildar $($svc.Image)"
        return $false
    }
}

function Restart-Service {
    param([string]$serviceName)

    $svc = $services[$serviceName]
    if (-not $svc) {
        Write-Error "Servico '$serviceName' nao encontrado"
        return $false
    }

    Write-Info "Reiniciando $($svc.K8sName)..."

    # Deletar pods para forçar recriação com nova imagem
    $output = kubectl delete pods -n $namespace -l app=$($svc.K8sName) 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Success "$($svc.K8sName) reiniciado (pods deletados, serao recriados)"
        return $true
    } else {
        Write-Error "Falha ao reiniciar $($svc.K8sName): $output"
        return $false
    }
}

function Set-LogLevel {
    param(
        [string]$serviceName,
        [string]$level
    )

    $validLevels = @("Trace", "Debug", "Information", "Warning", "Error", "Critical")
    if ($level -notin $validLevels) {
        Write-Error "Nivel invalido: '$level'. Use: $($validLevels -join ', ')"
        return
    }

    if ($serviceName -eq "all") {
        foreach ($key in $services.Keys | Sort-Object) {
            $svc = $services[$key]
            Write-Info "Setando log level '$level' em $($svc.K8sName)..."
            kubectl set env deployment/$($svc.K8sName) -n $namespace "Logging__LogLevel__Default=$level" 2>&1 | Out-Null
            Write-Success "$($svc.K8sName) -> $level"
        }
    } else {
        $svc = $services[$serviceName]
        if (-not $svc) {
            Write-Error "Servico '$serviceName' nao encontrado"
            return
        }
        Write-Info "Setando log level '$level' em $($svc.K8sName)..."
        kubectl set env deployment/$($svc.K8sName) -n $namespace "Logging__LogLevel__Default=$level" 2>&1 | Out-Null
        Write-Success "$($svc.K8sName) -> $level"
    }

    Write-Host "`nOs pods serao reiniciados automaticamente com o novo log level." -ForegroundColor Gray
}

function Redeploy-Service {
    param([string]$serviceName)

    Write-Host "`n--- Redeploying: $serviceName ---" -ForegroundColor Cyan

    if (Build-Service $serviceName) {
        Restart-Service $serviceName
    }
}

function Redeploy-All {
    Write-Host "`n--- Redeploying ALL services ---" -ForegroundColor Cyan

    # Reaplicar manifests K8s (pegar mudanças em YAMLs)
    Write-Info "Reaplicando manifests Kubernetes..."
    $kustomizePath = Join-Path $ProjectRoot "infra/k8s/overlays/dev"
    kubectl apply -k $kustomizePath 2>&1 | Out-Null
    Write-Success "Manifests atualizados"

    foreach ($serviceName in $services.Keys | Sort-Object) {
        Redeploy-Service $serviceName
    }
}

function Start-PortForward {
    Write-Host "`nEscolha o que expor:" -ForegroundColor White
    Write-Host "  [1] Gateway (porta 5000) - acesso as APIs" -ForegroundColor Yellow
    Write-Host "  [2] RabbitMQ Management (porta 15672)" -ForegroundColor Yellow
    Write-Host "  [3] MongoDB (porta 27017)" -ForegroundColor Yellow
    Write-Host "  [4] Gateway + RabbitMQ + MongoDB (todos)" -ForegroundColor Yellow
    Write-Host ""

    $choice = Read-Host "Escolha"

    $commands = @()
    switch ($choice) {
        "1" { $commands = @("kubectl port-forward -n $namespace svc/gateway 5000:80") }
        "2" { $commands = @("kubectl port-forward -n $namespace svc/rabbitmq 15672:15672") }
        "3" { $commands = @("kubectl port-forward -n $namespace svc/mongodb 27017:27017") }
        "4" { $commands = @(
                "kubectl port-forward -n $namespace svc/gateway 5000:80",
                "kubectl port-forward -n $namespace svc/rabbitmq 15672:15672",
                "kubectl port-forward -n $namespace svc/mongodb 27017:27017"
            )
        }
        default {
            Write-Error "Opcao invalida"
            return
        }
    }

    if ($commands.Count -eq 1) {
        Write-Info "Iniciando port-forward..."
        Write-Host "Pressione Ctrl+C para encerrar`n" -ForegroundColor Gray
        Invoke-Expression $commands[0]
    } else {
        Write-Info "Iniciando port-forwards em background..."
        foreach ($cmd in $commands) {
            Start-Process powershell -ArgumentList "-NoProfile -Command `"$cmd`"" -WindowStyle Minimized
            $port = if ($cmd -match "(\d+):") { $Matches[1] } else { "?" }
            Write-Success "Port-forward na porta $port iniciado (janela minimizada)"
        }
        Write-Host "`nURLs disponiveis:" -ForegroundColor White
        Write-Host "  Gateway:   http://localhost:5000" -ForegroundColor Gray
        Write-Host "  RabbitMQ:  http://localhost:15672 (guest/guest)" -ForegroundColor Gray
        Write-Host "  MongoDB:   localhost:27017" -ForegroundColor Gray
        Write-Host ""
    }
}

function Show-Menu {
    Write-Host "`nO que voce deseja fazer?" -ForegroundColor White
    Write-Host ""
    Write-Host "  [1] Redeploy de TODOS os servicos" -ForegroundColor Yellow
    Write-Host "  [2] Redeploy de um servico especifico" -ForegroundColor Yellow
    Write-Host "  [3] Apenas REBUILD (sem restart)" -ForegroundColor Yellow
    Write-Host "  [4] Apenas RESTART (sem rebuild)" -ForegroundColor Yellow
    Write-Host "  [5] Listar servicos" -ForegroundColor Yellow
    Write-Host "  [6] Alterar nivel de LOG" -ForegroundColor Yellow
    Write-Host "  [7] Port-forward (expor servicos localmente)" -ForegroundColor Yellow
    Write-Host "  [0] Sair" -ForegroundColor Gray
    Write-Host ""

    $choice = Read-Host "Escolha uma opcao"

    switch ($choice) {
        "1" {
            Redeploy-All
        }
        "2" {
            Show-ServiceList
            $svcName = Read-Host "Digite o nome do servico"
            if ($services.ContainsKey($svcName)) {
                Redeploy-Service $svcName
            } else {
                Write-Error "Servico '$svcName' nao encontrado"
            }
        }
        "3" {
            Show-ServiceList
            $svcName = Read-Host "Digite o nome do servico (ou 'all' para todos)"
            if ($svcName -eq "all") {
                foreach ($s in $services.Keys) {
                    Build-Service $s
                }
            } elseif ($services.ContainsKey($svcName)) {
                Build-Service $svcName
            } else {
                Write-Error "Servico '$svcName' nao encontrado"
            }
        }
        "4" {
            Show-ServiceList
            $svcName = Read-Host "Digite o nome do servico (ou 'all' para todos)"
            if ($svcName -eq "all") {
                foreach ($s in $services.Keys) {
                    Restart-Service $s
                }
            } elseif ($services.ContainsKey($svcName)) {
                Restart-Service $svcName
            } else {
                Write-Error "Servico '$svcName' nao encontrado"
            }
        }
        "5" {
            Show-ServiceList
            Show-Menu
        }
        "6" {
            Write-Host "`nNiveis disponiveis: Trace, Debug, Information, Warning, Error, Critical" -ForegroundColor Gray
            Write-Host "  Debug       = mostra health checks e logs detalhados" -ForegroundColor Gray
            Write-Host "  Information = padrao (requisicoes reais)" -ForegroundColor Gray
            Write-Host ""
            $level = Read-Host "Nivel de log"
            Show-ServiceList
            $svcName = Read-Host "Servico (ou 'all' para todos)"
            Set-LogLevel -serviceName $svcName -level $level
        }
        "7" {
            Start-PortForward
        }
        "0" {
            Write-Host "Saindo..." -ForegroundColor Gray
            return
        }
        default {
            Write-Error "Opcao invalida"
            Show-Menu
        }
    }
}

# =============================================================================
# INICIO
# =============================================================================

Write-Title

# Verificar minikube
$minikubeStatus = minikube status --format='{{.Host}}' 2>$null
if ($minikubeStatus -ne "Running") {
    Write-Error "Minikube nao esta em execucao!"
    Write-Host "Execute primeiro: .\scripts\quick-start.ps1" -ForegroundColor Yellow
    exit 1
}

# Configurar Docker do minikube
Write-Info "Configurando ambiente Docker do minikube..."
& minikube -p minikube docker-env --shell powershell 2>$null | Invoke-Expression

# Processar parametros
if ($PortForward) {
    Start-PortForward
    exit 0
}

if ($LogLevel) {
    $target = if ($Service) { $Service } else { "all" }
    Set-LogLevel -serviceName $target -level $LogLevel
    exit 0
}

if ($List) {
    Show-ServiceList
    exit 0
}

if ($All) {
    Redeploy-All
    exit 0
}

if ($Service) {
    if ($services.ContainsKey($Service)) {
        Redeploy-Service $Service
    } else {
        Write-Error "Servico '$Service' nao encontrado"
        Show-ServiceList
    }
    exit 0
}

# Menu interativo
Show-Menu

Write-Host "`n--- Status dos Pods ---" -ForegroundColor Cyan
kubectl get pods -n $namespace

Write-Host ""
