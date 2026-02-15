# 🧪 Guia Rápido de Testes

Referência rápida para executar testes unitários, integração, arquitetura e carga.

## 📋 Índice

- [Execução Rápida](#execução-rápida)
- [Por Categoria](#por-categoria)
- [Opções Avançadas](#opções-avançadas)
- [Interpretação de Resultados](#interpretação-de-resultados)

## 🚀 Execução Rápida

```bash
# Todos os testes
dotnet test -c Release

# Apenas suites principais
dotnet test -c Release --filter "Category!=LoadTest"
```

## 📂 Por Categoria

### Testes Unitários
```bash
dotnet test --filter "Category=Unit" -c Release
```

**Incluem**:
- Parser XML
- Idempotência (hash validation)
- Compressão e hashing
- Lógica de negócio

**Tempo**: ~5-10 segundos

### Testes de Integração
```bash
dotnet test --filter "Category=Integration" -c Release
```

**Incluem**:
- Upload API
- Idempotência com SQLite in-memory
- Padrão Repository

**Tempo**: ~10-15 segundos

### Testes de Arquitetura ⭐ (Novo!)
```bash
dotnet test --filter "Category=Architecture" -c Release --logger "console;verbosity=detailed"
```

**Incluem**:
- Validação de camadas (Clean Architecture)
- Abstrações (Interfaces)
- Convenção de nomes
- Acoplamento
- Padrão CQRS
- 40+ validações

**Tempo**: ~5-10 segundos

**Documentação completa**: [docs/ARCHITECTURE_TESTS.md](docs/ARCHITECTURE_TESTS.md)

### Testes de Carga ⚡ (Requer API rodando)
```bash
# Antes: iniciar API
docker compose up -d

# Testes de carga
dotnet test --filter "Category=LoadTest" -c Release --logger "console;verbosity=detailed"
```

**Incluem**:
- Carga constante
- Ramp-up progressivo
- Stress test
- Leitura com paginação
- Misto (upload + read)
- Validação de throughput

**Tempo**: ~5-10 minutos (depende dos cenários)

**Documentação completa**: [docs/LOAD_TESTING.md](docs/LOAD_TESTING.md)

## 🎯 Testes Específicos

### Teste individual por nome
```bash
dotnet test --filter "Name=Domain_Should_Not_Depend_On_Any_Other_Layer" -c Release
```

### Padrão de nome
```bash
# Todos os testes que começam com "Upload"
dotnet test --filter "Name~Upload" -c Release

# Todos de arquitetura de dependência
dotnet test --filter "Name~Depend" -c Release
```

## 📊 Opções Avançadas

### Com relatório de cobertura
```bash
dotnet test --collect:"XPlat Code Coverage" -c Release
```

Gera arquivo: `coverage.cobertura.xml`

### Com log detalhado
```bash
dotnet test -c Release --logger "console;verbosity=detailed" --logger "trx;LogFileName=results.trx"
```

### Paralelo (mais rápido)
```bash
dotnet test -c Release --parallel:auto
```

### Sem paralelismo (mais lento, mais confiável)
```bash
dotnet test -c Release --no-parallel
```

### Modo verbose (debugging)
```bash
dotnet test -c Release -p:DebugType=full -p:DebugSymbols=true --logger "console;verbosity=normal" 2>&1 | tee test-output.log
```

### Filtro combinado
```bash
# Todos EXCETO carga
dotnet test --filter "Category!=LoadTest" -c Release

# Apenas arquitetura E layering
dotnet test --filter "Category=Architecture&Name~Depend" -c Release
```

## 📈 Interpretação de Resultados

### Sucesso ✅
```
Test Run Successful.
Total tests: 42
  Passed: 42
  Failed: 0
Execution time: 15.234 seconds
```

### Falha ❌
```
Test Run Failed.
Total tests: 42
  Passed: 40
  Failed: 2

Failed Tests:
  XmlParserTests.InvalidXml_Should_Throw_Exception
  UploadXml_ConstantLoad_Test
```

**Próximos passos**:
1. Ler mensagem de erro completa
2. Verificar logs: `docker compose logs`
3. Executar teste isolado: `dotnet test --filter "Name=NomeDoTeste"`

## 📋 Checklist de Testes

Antes de fazer commit:

- [ ] ✅ Testes unitários passam
- [ ] ✅ Testes de integração passam
- [ ] ✅ Testes de arquitetura passam
- [ ] ✅ Sem warnings/erros de compilação
- [ ] ✅ Cobertura > 80%

Para CI/CD:

- [ ] ✅ Todos os testes em modo Release
- [ ] ✅ Docker compose up -d (para testes de integração)
- [ ] ✅ Testes de carga (opcional, pode ser em job separado)

## 🔧 Setup Inicial

```bash
# 1. Restaurar dependências
dotnet restore

# 2. Build
dotnet build

# 3. Iniciar serviços (para integração + carga)
docker compose up -d

# 4. Rodar testes
dotnet test -c Release

# 5. Parar serviços
docker compose down
```

## 📊 Performance Expected

| Suite | Duração | Obs |
|-------|---------|-----|
| Unitários | 5-10s | Rápido |
| Integração | 10-15s | Medium |
| Arquitetura | 5-10s | Reflection |
| Carga | 5-10min | Depende dos cenários |
| **Tudo** | ~30min | Paralelo máximo |

## 🎓 Dicas

### 1. Executar durante desenvolvimento
```bash
# Modo watch - reexecuta ao salvar
dotnet watch test --filter "Name~MyFeature"
```

### 2. Teste rápido antes de commit
```bash
# Sem carga, paralelo
dotnet test --filter "Category!=LoadTest" -c Release --parallel:auto
```

### 3. Teste completo antes de push
```bash
# Tudo, sequencial, com logs
docker compose up -d
dotnet test -c Release --logger "console;verbosity=detailed" --no-parallel
docker compose down
```

### 4. Só arquitetura (rápido)
```bash
# Verifica integridade da estrutura (~3 segundos)
dotnet test --filter "Category=Architecture" -c Release
```

### 5. Gerar relatório HTML de cobertura
```bash
# Instalar ferramenta
dotnet tool install -g reportgenerator

# Rodar testes com cobertura
dotnet test --collect:"XPlat Code Coverage" -c Release

# Gerar relatório
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Abrir
open coverage-report/index.html
```

## 🚨 Troubleshooting Rápido

### "Connection refused" (testes de integração falham)
```bash
docker compose up -d
# Aguarde 10s para DB ficar pronto
sleep 10
dotnet test --filter "Category=Integration"
```

### Testes lentos
```bash
# Use Release + paralelo
dotnet test -c Release --parallel:auto
```

### False positives intermitentes
```bash
# Sem paralelismo
dotnet test --no-parallel -c Release
```

### Docker não roda
```bash
# Verificar status
docker ps -a

# Limpar containers antigos
docker compose down -v

# Reconstruir
docker compose up -d --build
```

## 📚 Links

- [Unit Tests](FiscalDocumentProcessor.Tests/Unit/)
- [Integration Tests](FiscalDocumentProcessor.Tests/Integration/)
- [Architecture Tests Guide](docs/ARCHITECTURE_TESTS.md)
- [Load Testing Guide](docs/LOAD_TESTING.md)

---

**Última atualização**: 2026-02-15
