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

Pré-requisitos: .NET 8 SDK e acesso a um PostgreSQL (local ou gerenciado).

### Variáveis de ambiente

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
Você pode fornecer `XSD_PATH` apontando para um arquivo `.xsd` ou diretório contendo XSDs. Se definido, o serviço validará o XML antes do parse e rejeitará uploads que não respeitem o schema.
```bash
export XSD_PATH="/path/to/schemas" # ou /path/to/schema.xsd
```
```

### Restaurar, buildar, testar

```bash
dotnet restore
dotnet build -c Release
Dotnet test -c Release
```

### Executar API

```bash
dotnet run --project FiscalDocumentProcessor.Api
```

Acesse Swagger em `https://localhost:7057/swagger` (em Development).

### Executar Worker

```bash
dotnet run --project FiscalDocumentProcessor.Worker
```

## 📦 Docker Compose (opcional)

Um `docker-compose.yml` simples para API + PostgreSQL é incluído (RabbitMQ é externo). Ajuste variáveis de ambiente conforme seu CloudAMQP.

```yaml
author: you
```

> Observação: RabbitMQ local **não é necessário**. O sistema usa **CloudAMQP**.

## 📚 Endpoints

- `GET /documentos` – paginação (`page`, `pageSize`) e filtros (`dataInicio`, `dataFim`, `cnpj`, `uf`, `tipoDocumento`).
- `GET /documentos/{id}` – detalhes (CNPJ mascarado por padrão).
- `PUT /documentos/{id}` – atualiza campos permitidos (`UF`, `DataEmissao`, `ValorTotal`, `Status`).
- `DELETE /documentos/{id}` – remove documento.
- `POST /documentos/upload` – `multipart/form-data` com arquivo XML.

## 🧪 Testes

- **Unitários**: parser XML, idempotência, publisher, lógica de backoff.
- **Integração**: idempotência real em **SQLite in-memory** (índices únicos simulados); publicar é mockado.

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

## 🗂️ Scripts SQL (alternativa às migrations)

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
- Testes de carga (k6/NBomber) e limites explícitos de throughput.
- Fila de **retry** dedicada com TTL.

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
