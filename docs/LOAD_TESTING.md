# 📊 Guia de Testes de Carga

Este documento descreve como executar, interpretar e otimizar os testes de carga do **FiscalDocumentProcessor** usando **NBomber**.

## 📋 Requisitos

- API rodando (via `docker compose up`)
- .NET 8 SDK
- Terminal/PowerShell

## 🚀 Execução Rápida

### Todos os testes de carga
```bash
dotnet test --filter "Category=LoadTest" -c Release --logger "console;verbosity=detailed"
```

### Teste específico
```bash
dotnet test --filter "Name=UploadXml_ConstantLoad_Test" -c Release
```

### Com relatório de cobertura
```bash
dotnet test --collect:"XPlat Code Coverage" --filter "Category=LoadTest" -c Release
```

## 📊 Cenários de Carga

### 1. **Carga Constante** (`UploadXml_ConstantLoad_Test`)
**Propósito**: Simula uso normal e consistente da API.

- **Duração**: 30 segundos
- **Carga**: 10 uploads simultâneos
- **Taxa esperada**: ~80-100 documentos/s
- **Critério de sucesso**: Taxa de sucesso ≥ 95%

**Use quando**:
- Testar baseline de performance
- Validar que a API suporta carga típica de produção
- Monitorar degradação entre releases

### 2. **Ramp-Up** (`UploadXml_RampUp_Test`)
**Propósito**: Identifica progressivamente quando a performance piora.

- **Duração**: 60 segundos
- **Carga**: Aumenta de 0 a 20 requisições simultâneas
- **Taxa esperada**: Aumento gradual até ~160-200 docs/s
- **Critério de sucesso**: Taxa de sucesso ≥ 90%

**Use quando**:
- Encontrar ponto de saturação (kneepoint)
- Ajustar limites de throughput
- Planejar capacidade de infraestrutura

**Interpretar resultados**:
- Se taxa de sucesso cai abruptamente em N requisições = limite identificado
- Latência deve aumentar gradualmente, não abruptamente

### 3. **Stress Test** (`UploadXml_Stress_Test`)
**Propósito**: Testa comportamento sob carga extrema.

- **Duração**: 20 segundos
- **Carga**: 50 uploads simultâneos
- **Taxa esperada**: 400+ documentos/s (pode falhar)
- **Critério de sucesso**: Taxa de sucesso ≥ 80%

**Use quando**:
- Testar limites absolutos
- Validar tratamento de erros sob pressão
- Verificar se a API se recupera

**⚠️ Avisos**:
- Este teste pode retornar 503 Service Unavailable
- Conexões podem expirar (timeout)
- Normal observar aumento de falhas

### 4. **Leitura com Paginação** (`GetDocumentos_Read_Load_Test`)
**Propósito**: Valida performance de consultas.

- **Duração**: 30 segundos
- **Carga**: 15 leituras simultâneas
- **Taxa esperada**: ~150-200 req/s
- **Critério de sucesso**: Taxa de sucesso ≥ 98%

**Use quando**:
- Validar índices de banco de dados
- Testar filtros (data, CNPJ, UF)
- Monitorar queries lentas

### 5. **Carga Mista** (`Mixed_Upload_Read_Load_Test`)
**Propósito**: Simula cenário realista com upload e leitura.

- **Duração**: 30 segundos
- **Carga**: 5 uploads + 5 leituras simultâneas
- **Proporção**: 50% POST, 50% GET
- **Critério de sucesso**: Taxa de sucesso ≥ 90%

**Use quando**:
- Testar interferência entre operações
- Simular produção realista
- Validar pool de conexões

### 6. **Throughput Limitado** (`UploadXml_Throughput_LimitTest`)
**Propósito**: Garante throughput mínimo de 100 req/s.

- **Duração**: 30 segundos
- **Carga**: 20 uploads simultâneos
- **Limite**: 80 req/s mínimo (80% de 100)
- **Métrica**: Requisições por segundo (RPS)

**Use quando**:
- Definir SLA (Service Level Agreement)
- Validar degradação entre releases
- Planejar autoscaling

## 📈 Interpretando Resultados

### Métricas principais

```
Scenario: upload_xml_constant
  OK:           2400 (95%)
  Fail:         120 (5%)
  Latency:
    Average:    45 ms
    P50:        42 ms
    P95:        78 ms
    P99:        156 ms
  RPS:
    Average:    80 req/s
    Min:        75 req/s
    Max:        85 req/s
```

**O que significa**:
- **OK/Fail**: Taxa de sucesso (rejeitar se < 95%)
- **Average latency**: Tempo médio de resposta (< 100ms é bom)
- **P95/P99**: Percentis (95% das respostas em 78ms)
- **RPS**: Throughput real da API

### Análise de problemas

| Sintoma | Possível causa | Solução |
|---------|---|---|
| Taxa de sucesso cai para < 90% | API sobrecarregada | Aumentar CPU/memória ou otimizar código |
| Latência média > 200ms | Queries lentas no DB | Adicionar índices ou cache |
| RPS não aumenta com carga | Pool de conexões cheio | Aumentar `MaxPoolSize` do Npgsql |
| P99 >> P95 | Caudas latentes | Investigar outliers (GC, lock contention) |
| Erro 429 (Too Many Requests) | Rate limiting ativo | Ajustar limites ou aguardar |
| Erro 503 (Unavailable) | Serviço caiu | Reiniciar containers, verificar logs |

## 🔧 Ajustando Testes

### Aumentar duração
```csharp
// Antes
Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(30))

// Depois
Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(120))
```

### Aumentar carga
```csharp
// Antes (10 requisições simultâneas)
Simulation.KeepConstant(copies: 10, ...)

// Depois (50 requisições simultâneas)
Simulation.KeepConstant(copies: 50, ...)
```

### Mudar intervalo de relatório
```bash
dotnet test --filter "Name=UploadXml_ConstantLoad_Test" -c Release -- \
  --reporting-interval=1000  # relatório a cada 1 segundo em ms
```

## 📊 Comparação de Performance

Execute o mesmo teste em diferentes datas para detectar regressão:

```bash
# Dia 1
dotnet test -f "Name=UploadXml_ConstantLoad_Test" > day1-results.txt

# Dia 2 (após otimizações)
dotnet test -f "Name=UploadXml_ConstantLoad_Test" > day2-results.txt

# Comparar manualmente
diff day1-results.txt day2-results.txt
```

## 🎯 Metas de Performance

| Métrica | Alvo | Mínimo Aceitável |
|---------|------|---|
| Taxa de sucesso (carga constante) | 98% | 95% |
| Taxa de sucesso (ramp-up) | 95% | 90% |
| Taxa de sucesso (stress) | 85% | 80% |
| Latência média (upload) | < 50ms | < 100ms |
| Latência P95 (upload) | < 80ms | < 150ms |
| Throughput (upload) | > 150 req/s | > 100 req/s |
| Throughput (leitura) | > 300 req/s | > 200 req/s |

## 🚨 Troubleshooting

### Teste falha com "Connection refused"
```
❌ HTTP request to http://localhost:8080/documentos/upload failed
```

**Solução**: Iniciar API
```bash
docker compose up -d
```

### Muitos timeouts (5s)
```
❌ Task timeout after 00:00:05
```

**Solução**: Aumentar timeout no teste
```csharp
var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
```

### Taxa de sucesso muito baixa
- Verificar logs da API: `docker compose logs api-1`
- Validar conexão com PostgreSQL: `docker compose logs postgres`
- Verificar credenciais RabbitMQ

### RPS plateau em número fixo
- Pode ser limite de CPU do container
- Aumentar limits: editar `docker-compose.yml`
```yaml
services:
  api:
    cpus: '2'      # de 1 para 2
    mem_limit: 2g  # de 1g para 2g
```

## 🔍 Testes Avançados

### Executar múltiplos cenários em paralelo
```bash
# Todos os testes de carga
dotnet test --filter "Category=LoadTest" -c Release --no-build
```

### Filtrar por nome parcial
```bash
# Apenas testes de upload
dotnet test --filter "Name~Upload" -c Release

# Apenas testes de leitura
dotnet test --filter "Name~Read" -c Release
```

### Executar com variáveis de ambiente customizadas
```bash
API_URL=https://api.example.com:8080 \
dotnet test --filter "Name=UploadXml_ConstantLoad_Test" -c Release
```

## 📚 Referências

- [NBomber Official Docs](https://nbomber.com/)
- [Load Testing Best Practices](https://en.wikipedia.org/wiki/Load_testing)
- [SLA e SLO na prática](https://sre.google/sre-book/service-level-objectives/)

---

**Última atualização**: 2026-02-15
**Autor**: FiscalDocumentProcessor Team
