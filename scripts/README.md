# Scripts de Automação - AgroSolutions

Este diretório contém scripts PowerShell para gerenciar o ambiente de desenvolvimento local do projeto AgroSolutions no Kubernetes (minikube).

## Scripts Principais

| Script | Descrição | Quando usar |
|--------|-----------|-------------|
| `setup.ps1` | Instala todas as dependências necessárias (Docker, minikube, kubectl, k9s, Helm) | Apenas uma vez, na configuração inicial da máquina |
| `quick-start.ps1` | Inicia todo o ambiente (minikube + build + deploy) | Início do dia de trabalho ou após `stop.ps1` |
| `manage.ps1` | Gerenciamento de serviços: redeploy, logs, port-forward | Após alterações no código ou para gerenciar o ambiente |
| `test.ps1` | Verifica status rápido dos pods | Para checar se está tudo rodando |
| `stop.ps1` | Para o minikube (preserva dados) | Fim do dia ou para liberar recursos |
| `cleanup.ps1` | Remove namespace e/ou deleta minikube | Quando quer começar do zero |

---

## Detalhes de Cada Script

### `setup.ps1`

**Propósito:** Instalação única das ferramentas necessárias.

**O que instala:**
- Docker Desktop
- kubectl (CLI do Kubernetes)
- minikube (cluster Kubernetes local)
- k9s (visualizador de cluster no terminal)
- Helm (gerenciador de pacotes K8s)

**Uso:**
```powershell
# Executar como Administrador
.\scripts\setup.ps1
```

**Observações:**
- Requer PowerShell como Administrador
- Se instalar Docker Desktop, reinicie o PC após
- Execute apenas uma vez por máquina

---

### `quick-start.ps1`

**Propósito:** Iniciar todo o ambiente de desenvolvimento.

**O que faz:**
1. Verifica pré-requisitos (Docker, minikube)
2. Inicia o minikube (se não estiver rodando)
3. Builda as imagens Docker (se não existirem)
4. Faz deploy no Kubernetes (se não existir)
5. Mostra status dos pods

**Uso:**
```powershell
.\scripts\quick-start.ps1
```

**Observações:**
- Detecta automaticamente o que já foi feito e pula etapas
- Pode ser executado múltiplas vezes sem problemas
- Tempo estimado: 2-5 minutos (primeira vez), <30s (próximas)

---

### `manage.ps1`

**Propósito:** Script central para gerenciar serviços: redeploy, logs, port-forward.

**Modos de uso:**

```powershell
# Menu interativo
.\scripts\manage.ps1

# Redeploy de TODOS os serviços
.\scripts\manage.ps1 -All

# Redeploy de um serviço específico
.\scripts\manage.ps1 -Service gateway

# Listar serviços disponíveis
.\scripts\manage.ps1 -List

# Alterar nível de log
.\scripts\manage.ps1 -LogLevel Debug                     # Todos
.\scripts\manage.ps1 -LogLevel Debug -Service gateway     # Apenas gateway
.\scripts\manage.ps1 -LogLevel Information                # Voltar ao padrão

# Iniciar port-forward
.\scripts\manage.ps1 -PortForward
```

**Menu interativo:**
```
[1] Redeploy de TODOS os servicos
[2] Redeploy de um servico especifico
[3] Apenas REBUILD (sem restart)
[4] Apenas RESTART (sem rebuild)
[5] Listar servicos
[6] Alterar nivel de LOG
[7] Port-forward (expor servicos localmente)
[0] Sair
```

**Serviços disponíveis:**
| Nome | Imagem Docker | Deployment K8s |
|------|---------------|----------------|
| `gateway` | agrosolutions-gateway | gateway |
| `identity` | agrosolutions-identity-api | identity-api |
| `property` | agrosolutions-property-api | property-api |
| `dataingestion` | agrosolutions-dataingestion-api | dataingestion-api |
| `alert` | agrosolutions-alert-worker | alert-worker |

**Níveis de log disponíveis:** Trace, Debug, Information (padrão), Warning, Error, Critical

---

### `test.ps1`

**Propósito:** Verificação rápida do status do ambiente.

**O que mostra:**
- Status do minikube
- Status do namespace
- Lista de pods e seus estados
- Contagem de pods Running
- URLs de acesso

**Uso:**
```powershell
.\scripts\test.ps1
```

---

### `stop.ps1`

**Propósito:** Parar o minikube para liberar recursos do sistema.

**O que faz:**
- Para o cluster minikube
- Preserva todos os dados (pods, imagens, volumes)
- Próximo `quick-start.ps1` reinicia rapidamente

**Uso:**
```powershell
.\scripts\stop.ps1
```

**Observações:**
- Use no fim do dia de trabalho
- Libera ~4GB de RAM
- Dados são preservados

---

### `cleanup.ps1`

**Propósito:** Limpeza completa do ambiente.

**Modos de uso:**

```powershell
# Remove apenas o namespace (mantém minikube)
.\scripts\cleanup.ps1

# Remove tudo (incluindo minikube)
.\scripts\cleanup.ps1 -Full
```

**Quando usar:**
- Quando quer começar do zero
- Quando há problemas persistentes
- Para liberar espaço em disco

---

## Scripts Detalhados (pasta `detailed/`)

Scripts com mais verbose e controle granular. Úteis para debug.

| Script | Descrição |
|--------|-----------|
| `01-start-minikube.ps1` | Inicia minikube com configurações detalhadas |
| `02-build-images.ps1` | Builda imagens mostrando progresso |
| `03-deploy-k8s.ps1` | Deploy com status detalhado |
| `04-test-infrastructure.ps1` | Testes completos de conectividade |

---

## Fluxo de Trabalho Típico

### Primeiro dia (setup)
```powershell
.\scripts\setup.ps1        # Instalar ferramentas (1x)
# Reiniciar PC se Docker foi instalado
.\scripts\quick-start.ps1  # Iniciar ambiente
```

### Dia normal de desenvolvimento
```powershell
.\scripts\quick-start.ps1              # Iniciar ambiente
# ... desenvolver ...
.\scripts\manage.ps1 -Service identity     # Atualizar serviço alterado
.\scripts\test.ps1                     # Verificar status
.\scripts\stop.ps1                     # Fim do dia
```

### Recomeçar do zero
```powershell
.\scripts\cleanup.ps1 -Full  # Limpar tudo
.\scripts\quick-start.ps1    # Recriar ambiente
```

---

## Acessando os Serviços

### Opção 1: Via Ingress (recomendado)
```
http://agrosolutions.local
```
Requer configuração do arquivo hosts (ver documentação).

### Opção 2: Via Port-Forward
```powershell
kubectl port-forward -n agrosolutions svc/gateway 5000:80
```
Depois acesse: `http://localhost:5000`

### Opção 3: Via Minikube Tunnel
```powershell
minikube tunnel
```
Depois acesse: `http://localhost`

---

## Troubleshooting

### Minikube não inicia
```powershell
minikube delete
.\scripts\quick-start.ps1
```

### Pods em erro
```powershell
kubectl logs -n agrosolutions <nome-do-pod>
kubectl describe pod -n agrosolutions <nome-do-pod>
```

### Ver cluster visualmente
```powershell
k9s -n agrosolutions
```

### Recursos insuficientes
Edite `quick-start.ps1` e reduza `--memory` e `--cpus`.
