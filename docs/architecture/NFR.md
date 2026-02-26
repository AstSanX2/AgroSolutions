# Requisitos Nao Funcionais (NFR)

## 1. Escalabilidade

| Requisito | Como e Atendido |
|-----------|----------------|
| Escalar ingestao de sensores independentemente | Microsservico DataIngestion pode ter multiplas replicas; mensageria RabbitMQ distribui carga |
| Escalar processamento de alertas | Alert Worker pode ter multiplas instancias consumindo a mesma fila com prefetch |
| Escalar sem impacto nos outros servicos | Cada servico tem deploy independente via Kubernetes |

## 2. Disponibilidade

| Requisito | Como e Atendido |
|-----------|----------------|
| Servicos tolerantes a falhas | Health checks (liveness + readiness) com Kubernetes auto-restart |
| Comunicacao assincrona resiliente | RabbitMQ com mensagens persistentes; se consumidor cai, mensagens ficam na fila |
| Retry em conexoes | RabbitMQ EventBus com 5 tentativas de conexao com backoff |
| Desacoplamento temporal | Produtor e consumidor nao precisam estar online ao mesmo tempo |

## 3. Observabilidade

| Requisito | Como e Atendido |
|-----------|----------------|
| Metricas de aplicacao | OpenTelemetry com instrumentacao ASP.NET Core + HTTP Client |
| Metricas de negocio | Contadores customizados: `auth_login_total`, `sensor_readings_total`, `alerts_triggered_total` |
| Traces distribuidos | OpenTelemetry tracing em todos os servicos |
| Dashboards | Grafana com 3 dashboards provisionados (Overview, Sensores, Alertas) |
| Logs estruturados | Middleware de request logging com service name e status code |

## 4. Seguranca

| Requisito | Como e Atendido |
|-----------|----------------|
| Autenticacao | JWT com validacao de issuer, audience, lifetime e signing key |
| Hash de senhas | BCrypt (resistente a brute force) |
| Isolamento de dados | Propriedades filtradas por proprietarioId — usuario so acessa seus dados |
| Containers nao-root | Dockerfiles criam usuario `appuser` sem privilegios |
| Secrets separados | Kubernetes Secrets para connection strings e JWT secret |

## 5. Manutenibilidade

| Requisito | Como e Atendido |
|-----------|----------------|
| Separacao de responsabilidades | Clean Architecture: Domain / Infrastructure / API por servico |
| Validacao centralizada | FluentValidation com validators dedicados |
| Codigo compartilhado | Building Blocks (Common, EventBus) reutilizados entre servicos |
| Testes automatizados | 58 testes unitarios com xUnit + Moq + FluentAssertions |
| CI/CD | GitHub Actions com build + test automatico em push/PR |

## 6. Performance

| Requisito | Como e Atendido |
|-----------|----------------|
| Leituras de sensor com baixa latencia | Escrita direta no MongoDB sem transacoes complexas |
| Paginacao | Todos os endpoints de listagem com paginacao server-side |
| Processamento nao-bloqueante | Publicacao em filas nao bloqueia resposta da API (try/catch com warning log) |
| Prefetch otimizado | RabbitMQ com BasicQos prefetch=1 para distribuicao justa entre consumers |

## 7. Testabilidade

| Requisito | Como e Atendido |
|-----------|----------------|
| Testes unitarios | xUnit com Moq para mocks de repositorios e event bus |
| Validators testados | Testes parametrizados para cada regra de validacao |
| Alert rules testados | Testes para cada threshold de alerta |
| Controllers testados | Testes de integracao com mocks de dependencias |
| CI | Testes executados automaticamente no GitHub Actions |

## 8. Deploy

| Requisito | Como e Atendido |
|-----------|----------------|
| Containerizacao | Docker multi-stage build para todos os servicos |
| Orquestracao local | Docker Compose com health checks e dependencias |
| Orquestracao producao | Kubernetes manifests com liveness/readiness probes |
| Configuracao externalizada | Environment variables (12-factor app) |
| Provisioning automatico | Grafana dashboards e datasources provisionados via YAML |
