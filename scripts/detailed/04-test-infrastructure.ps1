# =============================================================================
# AgroSolutions - Script de Teste da Infraestrutura
# =============================================================================
# Este script testa se toda a infraestrutura Kubernetes esta funcionando
# corretamente.
#
# PRE-REQUISITOS:
# - Deploy realizado (execute 04-deploy-k8s.ps1)
#
# USO:
#   .\05-test-infrastructure.ps1
# =============================================================================

$ErrorActionPreference = "Continue"

# Diretorio raiz do projeto
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$namespace = "agrosolutions"

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

function Write-Fail {
    param([string]$message)
    Write-Host "[FALHOU] $message" -ForegroundColor Red
}

function Test-PodRunning {
    param(
        [string]$labelSelector,
        [string]$description
    )

    Write-Host "  Testando: $description... " -NoNewline

    $podStatus = kubectl get pods -n $namespace -l $labelSelector -o jsonpath="{.items[0].status.phase}" 2>$null
    $podReady = kubectl get pods -n $namespace -l $labelSelector -o jsonpath="{.items[0].status.conditions[?(@.type=='Ready')].status}" 2>$null

    if ($podStatus -eq "Running" -and $podReady -eq "True") {
        Write-Host "OK" -ForegroundColor Green
        return $true
    } elseif ($podStatus -eq "Running") {
        Write-Host "RUNNING (nao Ready ainda)" -ForegroundColor Yellow
        return $false
    } else {
        Write-Host "FALHOU (Status: $podStatus)" -ForegroundColor Red
        return $false
    }
}

function Test-ServiceExists {
    param(
        [string]$serviceName,
        [string]$description
    )

    Write-Host "  Testando: $description... " -NoNewline

    $service = kubectl get svc -n $namespace $serviceName -o jsonpath="{.metadata.name}" 2>$null

    if ($service -eq $serviceName) {
        Write-Host "OK" -ForegroundColor Green
        return $true
    } else {
        Write-Host "FALHOU" -ForegroundColor Red
        return $false
    }
}

function Test-MongoDBConnection {
    Write-Host "  Testando conexao com MongoDB... " -NoNewline

    # Criar um pod temporario para testar conexao
    $testResult = kubectl run mongo-test --rm -i --restart=Never `
        -n $namespace `
        --image=mongo:7 `
        -- mongosh mongodb://mongodb:27017 --eval "db.adminCommand('ping')" 2>&1

    if ($testResult -match '"ok"\s*:\s*1') {
        Write-Host "OK" -ForegroundColor Green
        return $true
    } else {
        Write-Host "FALHOU" -ForegroundColor Red
        return $false
    }
}

function Test-RabbitMQConnection {
    Write-Host "  Testando conexao com RabbitMQ... " -NoNewline

    # Criar um pod temporario para testar conexao via curl
    $testResult = kubectl run rabbitmq-test --rm -i --restart=Never `
        -n $namespace `
        --image=curlimages/curl `
        -- curl -s -u guest:guest http://rabbitmq:15672/api/overview 2>&1

    if ($testResult -match '"cluster_name"') {
        Write-Host "OK" -ForegroundColor Green
        return $true
    } else {
        Write-Host "FALHOU" -ForegroundColor Red
        return $false
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

              Teste da Infraestrutura Kubernetes

"@ -ForegroundColor Green

$totalTests = 0
$passedTests = 0

# =============================================================================
# 1. VERIFICAR NAMESPACE
# =============================================================================
Write-Step "1. Verificando Namespace"

$nsExists = kubectl get namespace $namespace -o jsonpath="{.metadata.name}" 2>$null
if ($nsExists -eq $namespace) {
    Write-Success "Namespace '$namespace' existe"
    $passedTests++
} else {
    Write-Fail "Namespace '$namespace' nao encontrado"
}
$totalTests++

# =============================================================================
# 2. VERIFICAR PODS DE INFRAESTRUTURA
# =============================================================================
Write-Step "2. Verificando Pods de Infraestrutura"

$infraPods = @(
    @{Label = "app=mongodb"; Description = "MongoDB"},
    @{Label = "app=rabbitmq"; Description = "RabbitMQ"}
)

foreach ($pod in $infraPods) {
    if (Test-PodRunning -labelSelector $pod.Label -description $pod.Description) {
        $passedTests++
    }
    $totalTests++
}

# =============================================================================
# 3. VERIFICAR PODS DE APLICACAO
# =============================================================================
Write-Step "3. Verificando Pods de Aplicacao"

$appPods = @(
    @{Label = "app=identity-api"; Description = "Identity API"},
    @{Label = "app=property-api"; Description = "Property API"},
    @{Label = "app=dataingestion-api"; Description = "DataIngestion API"},
    @{Label = "app=alert-worker"; Description = "Alert Worker"},
    @{Label = "app=gateway"; Description = "API Gateway"}
)

foreach ($pod in $appPods) {
    if (Test-PodRunning -labelSelector $pod.Label -description $pod.Description) {
        $passedTests++
    }
    $totalTests++
}

# =============================================================================
# 4. VERIFICAR SERVICES
# =============================================================================
Write-Step "4. Verificando Services"

$services = @(
    @{Name = "mongodb"; Description = "MongoDB Service"},
    @{Name = "rabbitmq"; Description = "RabbitMQ Service"},
    @{Name = "identity-api"; Description = "Identity API Service"},
    @{Name = "property-api"; Description = "Property API Service"},
    @{Name = "dataingestion-api"; Description = "DataIngestion API Service"},
    @{Name = "gateway"; Description = "Gateway Service"}
)

foreach ($svc in $services) {
    if (Test-ServiceExists -serviceName $svc.Name -description $svc.Description) {
        $passedTests++
    }
    $totalTests++
}

# =============================================================================
# 5. TESTAR CONECTIVIDADE (OPCIONAL - PODE DEMORAR)
# =============================================================================
Write-Step "5. Testando Conectividade"

Write-Host "  (Esses testes podem demorar alguns segundos...)" -ForegroundColor Gray

# Testar MongoDB
if (Test-MongoDBConnection) {
    $passedTests++
}
$totalTests++

# Testar RabbitMQ
if (Test-RabbitMQConnection) {
    $passedTests++
}
$totalTests++

# =============================================================================
# 6. RESUMO DOS TESTES
# =============================================================================
Write-Step "6. Resumo dos Testes"

$passRate = [math]::Round(($passedTests / $totalTests) * 100, 1)

Write-Host "`nResultado: $passedTests/$totalTests testes passaram ($passRate%)" -ForegroundColor White
Write-Host "-" * 50

if ($passedTests -eq $totalTests) {
    Write-Host @"

TODOS OS TESTES PASSARAM!

Sua infraestrutura Kubernetes esta funcionando corretamente.

COMO ACESSAR:

1. Abra um novo terminal e execute (deixe rodando):
   kubectl port-forward -n agrosolutions svc/gateway 5000:80

2. Acesse no navegador:
   http://localhost:5000/health

3. Para visualizar o cluster:
   k9s -n agrosolutions

4. Para ver os logs de um servico:
   kubectl logs -n agrosolutions -l app=identity-api -f

"@ -ForegroundColor Green

} elseif ($passRate -ge 70) {
    Write-Host @"

A MAIORIA DOS TESTES PASSOU!

Alguns componentes ainda estao inicializando.
Aguarde alguns minutos e execute o script novamente.

Para verificar o status dos pods:
  kubectl get pods -n agrosolutions

Para ver eventos do cluster:
  kubectl get events -n agrosolutions --sort-by='.lastTimestamp'

"@ -ForegroundColor Yellow

} else {
    Write-Host @"

VARIOS TESTES FALHARAM!

Verifique o status dos pods:
  kubectl get pods -n agrosolutions

Verifique os logs dos pods com problema:
  kubectl logs -n agrosolutions <nome-do-pod>

Verifique eventos:
  kubectl get events -n agrosolutions --sort-by='.lastTimestamp'

Possiveis causas:
- Imagens Docker nao foram buildadas (execute 03-build-images.ps1)
- Recursos insuficientes no minikube
- Erros de configuracao nos manifests

"@ -ForegroundColor Red
}

# =============================================================================
# 7. DETALHES DOS PODS
# =============================================================================
Write-Step "7. Status Detalhado dos Pods"

kubectl get pods -n $namespace -o wide

Write-Host "`n"
