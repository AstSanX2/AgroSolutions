# Para todos os servicos Docker Compose
Write-Host "Parando servicos AgroSolutions..." -ForegroundColor Cyan

docker-compose down

Write-Host "Servicos parados com sucesso!" -ForegroundColor Green
