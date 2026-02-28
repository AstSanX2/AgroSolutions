# Inicia todos os servicos com Docker Compose
Write-Host "Iniciando servicos AgroSolutions..." -ForegroundColor Cyan

docker-compose up -d

Write-Host ""
Write-Host "Servicos iniciados com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "Acesse:" -ForegroundColor Yellow
Write-Host "  - Gateway:   http://localhost:5000"
Write-Host "  - RabbitMQ:  http://localhost:15672 (guest/guest)"
Write-Host "  - Grafana:   http://localhost:3000 (admin/admin)"
Write-Host "  - Swagger Identity:      http://localhost:5000/api/auth/swagger"
Write-Host "  - Swagger Property:      http://localhost:5000/api/properties/swagger"
Write-Host "  - Swagger DataIngestion: http://localhost:5000/api/sensors/swagger"
