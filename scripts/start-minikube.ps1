# Inicia o ambiente Kubernetes local com Minikube
Write-Host "Iniciando Minikube..." -ForegroundColor Cyan

minikube start --memory=4096 --cpus=2

Write-Host "Habilitando Ingress..." -ForegroundColor Cyan
minikube addons enable ingress

Write-Host "Configurando Docker do Minikube..." -ForegroundColor Cyan
& minikube -p minikube docker-env --shell powershell | Invoke-Expression

Write-Host "Construindo imagens Docker..." -ForegroundColor Cyan
docker-compose build

Write-Host "Aplicando manifests Kubernetes..." -ForegroundColor Cyan
kubectl apply -k infra/k8s/overlays/dev

Write-Host "Aguardando pods ficarem prontos..." -ForegroundColor Yellow
kubectl wait --for=condition=ready pod -l app=identity-api -n agrosolutions-dev --timeout=120s
kubectl wait --for=condition=ready pod -l app=property-api -n agrosolutions-dev --timeout=120s
kubectl wait --for=condition=ready pod -l app=dataingestion-api -n agrosolutions-dev --timeout=120s
kubectl wait --for=condition=ready pod -l app=gateway -n agrosolutions-dev --timeout=120s

Write-Host ""
Write-Host "Ambiente Kubernetes pronto!" -ForegroundColor Green
Write-Host "Iniciando tunnel para acesso externo..." -ForegroundColor Yellow
Write-Host "  Acesse o Gateway em: http://localhost:5000" -ForegroundColor Cyan
Write-Host ""

minikube tunnel
