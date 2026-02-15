# FiscalDocumentProcessor

Backend em **.NET 8** para **ingestão e processamento de documentos fiscais XML (NFe, CTe e NFSe)** usando **Clean Architecture** e mensageria **RabbitMQ (CloudAMQP)**.

## 🧱 Projetos

- `FiscalDocumentProcessor.Api` – ASP.NET Web API (upload e consulta)
- `FiscalDocumentProcessor.Application` – Casos de uso, DTOs, handlers (MediatR)
- `FiscalDocumentProcessor.Domain` – Entidades e enums
- `FiscalDocumentProcessor.Infrastructure` – EF Core (PostgreSQL), RabbitMQ Publisher, serviços
- `FiscalDocumentProcessor.Worker` – Worker Service (Consumer) com retry, backoff e DLQ
- `FiscalDocumentProcessor.Tests` – Testes (NUnit + FluentAssertions)

## 🗄️ Banco de Dados: **PostgreSQL**

Escolhi **PostgreSQL** por ser open-source, robusto para carga OLTP, excelente suporte a índices e transações, e possuir provider EF Core maduro. Os dados são essencialmente relacionais (consultas paginadas por período, CNPJ, UF e tipo), beneficiando-se de **índices** e **consistência**. O XML completo é armazenado **compactado (GZip)** em `bytea`, mantendo custo de armazenamento baixo. Índices **únicos** em `ChaveAcesso` e **principalmente** em `HashXml (SHA-256)` garantem **idempotência**.

## 🔐 Segurança

- **XXE protegido** via `XmlReaderSettings` com `DtdProcessing=Prohibit` e `XmlResolver=null` ao parsear.
- **Sanitização**: apenas retornos mascarados de CNPJ (`**`), sem expor XML por padrão.
- **Secrets via variáveis de ambiente** (RabbitMQ, conexão DB). Nenhuma credencial versionada.
- **TLS obrigatório** para CloudAMQP (usa `amqps://`).

## 🔁 Idempotência

- Hash **SHA-256** do conteúdo XML.
- Índice **único** em `HashXml` (e `ChaveAcesso` quando existir).
- Inserção transacional via EF Core; duplicate key -> tratado e retornado como **já existente**.
- **Consumer** idempotente: se `Status=Processado`, apenas ack.

## 🐇 RabbitMQ (CloudAMQP)

- Conexão por **URI AMQPS** via `RABBITMQ_URI`.
- Publicação em **exchange** `fiscal.documents` com **routing key** `document.processed` e **publisher confirms**.
- **Worker** cria topologia (queue + DLX), usa **ack manual**, **retry com backoff exponencial** e **DLQ**.

**Evento publicado** `DocumentoFiscalProcessadoEvent`:
```json
{
  "DocumentoId": "guid",
  "TipoDocumento": "NFe|CTe|NFSe",
  "ChaveAcesso": "string",
  "DataProcessamento": "2026-02-14T12:00:00Z"
}
```

## 🚀 Como executar

### Opção 1: Docker Compose (Recomendado)

Pré-requisitos: **Docker** e **Docker Compose** instalados.

Para iniciar o projeto localmente com todos os serviços (API, PostgreSQL e Worker):

```bash
docker compose up -d --build
```

Este comando irá:
- Construir as imagens da API e do Worker
- Iniciar um container PostgreSQL
- Iniciar os containers da API e Worker em background

Para parar os serviços:

```bash
docker compose down
```

Para visualizar os logs:

```bash
docker compose logs -f
```

**Variáveis de ambiente:** O `docker-compose.yml` já contém as configurações necessárias. Caso queira customizar (ex: credenciais do RabbitMQ), edite o arquivo ou passe variáveis de ambiente na linha de comando.

### Opção 2: Execução local (sem Docker)

Pré-requisitos: **.NET 8 SDK**, **PostgreSQL** e acesso a um **RabbitMQ (CloudAMQP)**.

#### Variáveis de ambiente

```bash
# Banco
export FISCALDB_CONNECTION="Host=localhost;Port=5432;Database=fiscaldb;Username=postgres;Password=postgres"

# RabbitMQ (CloudAMQP)
export RABBITMQ_URI="amqps://<user>:<pass>@<host>/<vhost>"
export RABBITMQ_EXCHANGE="fiscal.documents"
export RABBITMQ_ROUTING_KEY="document.processed"
export RABBITMQ_QUEUE="fiscal.documents.processor"
export RABBITMQ_DLX="fiscal.documents.dlx"

# Validação opcional de schema
export XSD_PATH="/path/to/schemas" # ou /path/to/schema.xsd
```

#### Restaurar, buildar e testar

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

#### Executar API

```bash
dotnet run --project FiscalDocumentProcessor.Api
```

Acesse Swagger em `https://localhost:8080/swagger` (em Development).

#### Executar Worker

```bash
dotnet run --project FiscalDocumentProcessor.Worker
```

## 📮 Importar Collection no Postman

Para facilitar os testes da API, uma **Collection do Postman** está disponível em `docs/FiscalDocumentProcessor.postman_collection.json`.

### Como importar:

1. Abra o **Postman**
2. Clique em **Import** (ou Ctrl+O)
3. Selecione a aba **Upload Files** e escolha `docs/FiscalDocumentProcessor.postman_collection.json`
4. A coleção será carregada com todos os endpoints pré-configurados

Todos os endpoints (GET, POST, PUT, DELETE) estarão prontos para uso com variáveis de ambiente e exemplos de requisição.

## 📚 Endpoints

- `GET /documentos` – paginação (`page`, `pageSize`) e filtros (`dataInicio`, `dataFim`, `cnpj`, `uf`, `tipoDocumento`).
- `GET /documentos/{id}` – detalhes (CNPJ mascarado por padrão).
- `PUT /documentos/{id}` – atualiza campos permitidos (`UF`, `DataEmissao`, `ValorTotal`, `Status`).
- `DELETE /documentos/{id}` – remove documento.
- `POST /documentos/upload` – `multipart/form-data` com arquivo XML.

## 🧪 Testes

### Unitários e Integração
- **Unitários**: parser XML, idempotência, publisher, lógica de backoff.
- **Integração**: idempotência real em **SQLite in-memory** (índices únicos simulados); publicar é mockado.

### 🏗️ Testes de Arquitetura (NetArchTest)

Validações automáticas de **Clean Architecture**, padrões de design e conformidade com dependências:

#### Categorias de testes:

- **Camadas (Layering)**: Domain não depende de ninguém; Application não depende de Infrastructure; ordem correta
- **Abstração**: Repositories, Services e Handlers como interfaces
- **Convenção de nomes**: Commands, Queries, DTOs, Handlers com nomes corretos
- **CQRS**: Commands e Queries separados, handlers implementando MediatR
- **Acoplamento**: DbContext não em Application, Controllers apenas em Api
- **Coesão**: Entities em namespaces corretos, DTOs sem navigation properties

#### Como executar:

```bash
# Todos os testes de arquitetura
dotnet test --filter "Category=Architecture" -c Release --logger "console;verbosity=detailed"

# Teste específico
dotnet test --filter "Name=Domain_Should_Not_Depend_On_Any_Other_Layer" -c Release
```

**📖 Detalhes em**: [FiscalDocumentProcessor.Tests/Integration/ArchitectureTests.cs](FiscalDocumentProcessor.Tests/Integration/ArchitectureTests.cs) (40+ validações)

### 📊 Testes de Carga (NBomber)

O projeto inclui testes de carga abrangentes usando **NBomber** para avaliar desempenho de ingestão e consultas sob diferentes cenários:

#### Cenários disponíveis:

- **UploadXml_ConstantLoad_Test** – 10 uploads simultâneos por 30s (carga constante)
- **UploadXml_RampUp_Test** – Aumenta gradualmente de 1 a 20 req/s em 60s
- **UploadXml_Stress_Test** – 50 uploads simultâneos por 20s (teste de pico)
- **GetDocumentos_Read_Load_Test** – 15 leituras simultâneas por 30s
- **Mixed_Upload_Read_Load_Test** – 50% upload + 50% leitura (cenário realista)
- **UploadXml_Throughput_LimitTest** – Valida throughput mínimo de 100 req/s

#### Como executar:

```bash
# Todos os testes de carga
dotnet test --filter "Category=LoadTest" -c Release --logger "console;verbosity=detailed"

# Teste específico
dotnet test --filter "Name=UploadXml_ConstantLoad_Test" -c Release

# Com relatório de cobertura
dotnet test --collect:"XPlat Code Coverage" --filter "Category=LoadTest"
```

#### Métricas coletadas:

- Taxa de sucesso (%)
- Requisições por segundo (RPS)
- Latência média, P50, P95, P99
- Tempo de resposta (OK/Fail)
- Identificação de gargalos

**Nota**: Os testes de carga requerem que a API esteja rodando (`docker compose up`)

**📖 Detalhes em**:
- [docs/ARCHITECTURE_TESTS.md](docs/ARCHITECTURE_TESTS.md) – 40+ validações de Clean Architecture
- [docs/LOAD_TESTING.md](docs/LOAD_TESTING.md) – Guia completo de testes de carga
- [docs/TESTING_QUICK_REFERENCE.md](docs/TESTING_QUICK_REFERENCE.md) – Referência rápida de comandos

## 🩺 Health Checks

- `GET /health` – básico.

## 📈 Performance

- `async/await` em todo IO
- Parser usando **XmlReader** seguro
- Índices no banco (`ChaveAcesso`, `HashXml`)
- **Paginação obrigatória**
- Pool de conexões padrão do Npgsql

## 🔭 Observabilidade

- Logging estruturado com **Serilog** no console.

## 🗂️ Migrations EF Core (e scripts SQL)

As **migrations** já estão versionadas no projeto e são aplicadas automaticamente ao iniciar a **API** ou o **Worker** quando o provider é **PostgreSQL**. Para ambientes sem migrations (ex.: SQLite local), o `EnsureCreated` é usado apenas em desenvolvimento.

Se preferir aplicar manualmente ou em ambientes restritos, use o script abaixo como alternativa:

```sql
CREATE TABLE IF NOT EXISTS documentos_fiscais (
  Id uuid PRIMARY KEY,
  TipoDocumento text NOT NULL,
  ChaveAcesso varchar(60),
  CNPJEmitente varchar(20),
  CNPJDestinatario varchar(20),
  UF char(2),
  DataEmissao timestamp NULL,
  ValorTotal numeric(18,2) NULL,
  XmlOriginalGzip bytea NULL,
  HashXml varchar(128) NOT NULL,
  DataProcessamento timestamp NOT NULL,
  Status int NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS IX_documentos_chave ON documentos_fiscais(ChaveAcesso);
CREATE UNIQUE INDEX IF NOT EXISTS IX_documentos_hash ON documentos_fiscais(HashXml);
```

## 🔒 Estratégia para dados sensíveis

- Variáveis de ambiente em execução local e **secret stores** em nuvem.
- **Nunca** commitar credenciais/GUIDs reais.

## 🔧 Melhorias futuras

- Autenticação/Autorização (OAuth2/JWT) e políticas de mascaramento condicionado.
- Upload assíncrono via S3/Azure Blob e eventos.
- OpenTelemetry (traces/metrics) e dashboards.
- Fila de **retry** dedicada com TTL.
- Dashboard de monitoramento em tempo real para testes de carga.

---

> Comandos de referência:
>
> ```bash
> dotnet restore
> dotnet build
> dotnet test
> dotnet run --project FiscalDocumentProcessor.Api
> dotnet run --project FiscalDocumentProcessor.Worker
> ```
