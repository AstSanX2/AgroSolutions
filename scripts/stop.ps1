# =============================================================================
# AgroSolutions - Parar Ambiente
# =============================================================================
# Para o minikube (preserva dados para reiniciar depois).
#
# USO:
#   .\stop.ps1
# =============================================================================

Write-Host "`n=== AgroSolutions - Stop ===`n" -ForegroundColor Yellow

$minikubeStatus = minikube status --format='{{.Host}}' 2>$null

if ($minikubeStatus -ne "Running") {
    Write-Host "Minikube ja esta parado." -ForegroundColor Gray
    exit 0
}

Write-Host "Parando minikube..." -ForegroundColor Yellow
minikube stop

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[OK] Minikube parado com sucesso!" -ForegroundColor Green
    Write-Host "Seus dados foram preservados." -ForegroundColor Gray
    Write-Host "`nPara reiniciar: .\scripts\quick-start.ps1" -ForegroundColor Cyan
} else {
    Write-Host "`n[ERRO] Falha ao parar minikube" -ForegroundColor Red
}

Write-Host ""
