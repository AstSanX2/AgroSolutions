# =============================================================================
# AgroSolutions - Teste Rapido da Infraestrutura
# =============================================================================
# Verifica rapidamente se os pods estao rodando.
#
# USO:
#   .\test.ps1
# =============================================================================

$namespace = "agrosolutions"

Write-Host "`n=== AgroSolutions - Status ===`n" -ForegroundColor Cyan

# Verificar minikube
$minikubeStatus = minikube status --format='{{.Host}}' 2>$null
if ($minikubeStatus -eq "Running") {
    Write-Host "[OK] Minikube rodando" -ForegroundColor Green
} else {
    Write-Host "[X] Minikube parado" -ForegroundColor Red
    Write-Host "Execute: .\scripts\quick-start.ps1" -ForegroundColor Yellow
    exit 1
}

# Verificar namespace
$ns = kubectl get namespace $namespace -o jsonpath="{.metadata.name}" 2>$null
if ($ns -eq $namespace) {
    Write-Host "[OK] Namespace '$namespace' existe" -ForegroundColor Green
} else {
    Write-Host "[X] Namespace '$namespace' nao existe" -ForegroundColor Red
    Write-Host "Execute: .\scripts\quick-start.ps1" -ForegroundColor Yellow
    exit 1
}

# Status dos pods
Write-Host "`n--- Pods ---" -ForegroundColor White
kubectl get pods -n $namespace

# Contar pods running
$runningPods = (kubectl get pods -n $namespace --field-selector=status.phase=Running --no-headers 2>$null | Measure-Object -Line).Lines
$totalPods = (kubectl get pods -n $namespace --no-headers 2>$null | Measure-Object -Line).Lines

Write-Host "`n$runningPods/$totalPods pods Running" -ForegroundColor $(if ($runningPods -eq $totalPods) { "Green" } else { "Yellow" })

# Mostrar URLs de acesso
Write-Host "`n--- Acesso ---" -ForegroundColor White
Write-Host "Gateway: http://agrosolutions.local (se configurou hosts)" -ForegroundColor Gray
Write-Host "Ou use:  kubectl port-forward -n agrosolutions svc/gateway 5000:80" -ForegroundColor Gray
Write-Host ""
