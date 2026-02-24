# =============================================================================
# AgroSolutions - Script de Limpeza
# =============================================================================
# Este script remove todos os recursos do Kubernetes e opcionalmente
# para/deleta o minikube.
#
# USO:
#   .\cleanup.ps1           - Remove recursos K8s, mantem minikube
#   .\cleanup.ps1 -Full     - Remove tudo, incluindo minikube
# =============================================================================

param(
    [switch]$Full
)

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

Write-Host @"

    _                    ____        _       _   _
   / \   __ _ _ __ ___  / ___|  ___ | |_   _| |_(_) ___  _ __  ___
  / _ \ / _` | '__/ _ \ \___ \ / _ \| | | | | __| |/ _ \| '_ \/ __|
 / ___ \ (_| | | | (_) | ___) | (_) | | |_| | |_| | (_) | | | \__ \
/_/   \_\__, |_|  \___/ |____/ \___/|_|\__,_|\__|_|\___/|_| |_|___/
        |___/

              Limpeza do Ambiente

"@ -ForegroundColor Yellow

# =============================================================================
# 1. REMOVER RECURSOS KUBERNETES
# =============================================================================
Write-Step "1. Removendo recursos do Kubernetes"

$nsExists = kubectl get namespace $namespace 2>$null
if ($nsExists) {
    Write-Host "Deletando namespace '$namespace' e todos os recursos..." -ForegroundColor Yellow
    kubectl delete namespace $namespace --grace-period=30

    Write-Success "Namespace deletado"
} else {
    Write-Host "Namespace '$namespace' nao existe" -ForegroundColor Gray
}

if ($Full) {
    # =============================================================================
    # 2. PARAR MINIKUBE
    # =============================================================================
    Write-Step "2. Parando minikube"

    minikube stop
    Write-Success "Minikube parado"

    # =============================================================================
    # 3. DELETAR MINIKUBE (OPCIONAL)
    # =============================================================================
    Write-Step "3. Deletar cluster minikube?"

    $confirm = Read-Host "Deseja DELETAR o cluster minikube completamente? (s/n)"
    if ($confirm -eq 's') {
        minikube delete
        Write-Success "Cluster minikube deletado"
    } else {
        Write-Host "Cluster minikube preservado (apenas parado)" -ForegroundColor Yellow
    }
}

Write-Host @"

LIMPEZA CONCLUIDA!

Para recriar o ambiente:
  1. .\02-start-minikube.ps1
  2. .\03-build-images.ps1
  3. .\04-deploy-k8s.ps1

"@ -ForegroundColor Green
