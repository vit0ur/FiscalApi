# 🏗️ Guia de Testes de Arquitetura

Este documento descreve os testes de arquitetura do **FiscalDocumentProcessor** usando **NetArchTest.Rules**. Estes testes garantem que o projeto segue **Clean Architecture** e padrões de design estabelecidos.

## 📋 Requisitos

- .NET 8 SDK
- NUnit e NetArchTest.Rules (já inclusos no projeto)

## 🚀 Execução Rápida

### Todos os testes de arquitetura
```bash
dotnet test --filter "Category=Architecture" -c Release --logger "console;verbosity=detailed"
```

### Teste específico
```bash
dotnet test --filter "Name=Domain_Should_Not_Depend_On_Any_Other_Layer" -c Release
```

### Com relatório de cobertura
```bash
dotnet test --collect:"XPlat Code Coverage" --filter "Category=Architecture" -c Release
```

## 🏛️ Estrutura de Camadas (Clean Architecture)

O projeto segue a arquitetura em 5 camadas com restrições de dependência:

```
┌─────────────────────────────────────────────────┐
│   Presentation (Api & Worker)                   │
├─────────────────────────────────────────────────┤
│   Application (Use Cases, DTOs, Handlers)       │
├─────────────────────────────────────────────────┤
│   Infrastructure (EF Core, Repositories)        │
├─────────────────────────────────────────────────┤
│   Domain (Entities, Events, Enums)              │
└─────────────────────────────────────────────────┘
```

### Dependências permitidas

```
Domain (camada mais interna)
  ↑
Application (depende de Domain)
  ↑
Infrastructure (depende de Domain + Application)
  ↑
Api + Worker (dependem de todas as camadas)
```

### Dependências **proibidas** (testadas)

- ❌ Domain depende de qualquer outra camada
- ❌ Application depende de Infrastructure, Api ou Worker
- ❌ Infrastructure depende de Api ou Worker
- ❌ Api depende de Worker
- ❌ Worker depende de Api

## 📊 Testes de Layering

### 1. Domain Should Not Depend On Any Other Layer
**Objetivo**: Domain é a camada mais interna e pura, sem dependências externas.

```csharp
[Test]
public void Domain_Should_Not_Depend_On_Any_Other_Layer()
```

**Verifica**:
- Domain não importa Application
- Domain não importa Infrastructure
- Domain não importa Api
- Domain não importa Worker

**Falha se**: Algum tipo em Domain referencia tipos de outras camadas.

### 2. Application Should Not Depend On Infrastructure Or Api
**Objetivo**: Application contém lógica de negócio pura, desacoplada de detalhes de implementação.

```csharp
[Test]
public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
```

**Verifica**:
- Application não importa Infrastructure (sem DbContext, repositories concretos)
- Application não importa Api (sem Controllers)
- Application não importa Worker

**Falha se**: Application usa `DbContext` diretamente ou referencia `ControllerBase`.

### 3. Application Should Depend On Domain
**Objetivo**: Application usa definições de Domain (Entities, Events).

```csharp
[Test]
public void Application_Should_Depend_On_Domain()
```

**Verifica**:
- Application pode usar tipos de Domain

**Falha se**: Application não importa Domain (raro, mas inválido).

### 4. Infrastructure Should Depend On Application And Domain
**Objetivo**: Infrastructure implementa interfaces e padrões de Application.

```csharp
[Test]
public void Infrastructure_Should_Depend_On_Application_And_Domain()
```

**Verifica**:
- Infrastructure importa Application (interfaces de repositórios)
- Infrastructure importa Domain (entidades)

**Falha se**: Infrastructure não tem acesso a Application ou Domain.

### 5. Infrastructure Should Not Depend On Api Or Worker
**Objetivo**: Infrastructure é agnóstico à apresentação.

```csharp
[Test]
public void Infrastructure_Should_Not_Depend_On_Api_Or_Worker()
```

**Verifica**:
- Infrastructure não importa Api
- Infrastructure não importa Worker

**Falha se**: Infrastructure referencia Controllers ou Classes específicas de Worker.

### 6. Api Should Depend On Application Domain And Infrastructure
**Objetivo**: Api orquestra as camadas.

```csharp
[Test]
public void Api_Should_Depend_On_Application_Domain_And_Infrastructure()
```

**Verifica**:
- Api importa Application (comandos, queries)
- Api importa Domain (entidades)
- Api importa Infrastructure (implementações concretas)

**Falha se**: Api não tem acesso às camadas necessárias.

### 7. Worker Should Depend On Application Domain And Infrastructure
**Objetivo**: Worker orquestra as camadas como consumer de mensagens.

```csharp
[Test]
public void Worker_Should_Depend_On_Application_Domain_And_Infrastructure()
```

**Verifica**:
- Worker importa Application
- Worker importa Domain
- Worker importa Infrastructure

**Falha se**: Worker não consegue acessar handlers ou repositórios.

## 🔧 Testes de Abstração

### Repositories Should Be Abstracted As Interfaces
**Objetivo**: Padrão Repository - interfaces em Application, implementações em Infrastructure.

```csharp
[Test]
public void Repositories_Should_Be_Abstracted_As_Interfaces()
```

**Verifica**:
- Interfaces `IDocumentRepository` estão em `Application.Abstractions`
- Implementações `DocumentRepository` estão em `Infrastructure.Repositories`

**Falha se**: Repositório concreto está em Application.

### Services Should Be Abstracted As Interfaces
**Objetivo**: Padrão Strategy - interfaces em Application, implementações em Infrastructure.

```csharp
[Test]
public void Services_Should_Be_Abstracted_As_Interfaces()
```

**Verifica**:
- Interfaces `ICompressionService`, `IHashService` em `Application.Abstractions`
- Implementações em `Infrastructure.Services`

**Falha se**: Serviço concreto está em Application.

## 🎯 Testes de Convenção de Nomes

### Entity Classes Should Be In Entities Namespace
```csharp
[Test]
public void Entity_Classes_Should_Be_In_Entities_Namespace()
```

**Esperado**: `FiscalDocumentProcessor.Domain.Entities.DocumentoFiscal`

**Falha se**: Entity em `Domain.Models` ou outro namespace.

### Enums Should Be In Enums Namespace
```csharp
[Test]
public void Enums_Should_Be_In_Enums_Namespace()
```

**Esperado**: `FiscalDocumentProcessor.Domain.Enums.TipoDocumento`

**Falha se**: Enum em `Domain.Types` ou sem namespace separado.

### Events Should Be In Events Namespace
```csharp
[Test]
public void Events_Should_Be_In_Events_Namespace()
```

**Esperado**: `FiscalDocumentProcessor.Domain.Events.DocumentoFiscalProcessadoEvent`

**Falha se**: Event em `Domain` ou `Application.Events`.

### DTO Classes Should Be In DTOs Namespace
```csharp
[Test]
public void DTO_Classes_Should_Be_In_DTOs_Namespace()
```

**Esperado**: `FiscalDocumentProcessor.Application.DTOs.DocumentoFiscalDto`

**Falha se**: DTO em `Application.Models` ou sem sufixo `Dto`.

## 🔄 Testes de Padrão CQRS

### Command Handlers Should Implement IRequestHandler From MediatR
```csharp
[Test]
public void Command_Handlers_Should_Implement_IRequestHandler_From_MediatR()
```

**Verifica**:
- Handlers em `Features.Commands` implementam `IRequestHandler<TRequest>`
- Names terminam com `Handler`

**Exemplo**:
```csharp
public class CreateDocumentoCommandHandler : IRequestHandler<CreateDocumentoCommand, Guid>
{
    public async Task<Guid> Handle(CreateDocumentoCommand request, CancellationToken ct)
    {
        // ...
    }
}
```

### Query Handlers Should Implement IRequestHandler From MediatR
```csharp
[Test]
public void Query_Handlers_Should_Implement_IRequestHandler_From_MediatR()
```

**Verifica**:
- Handlers em `Features.Queries` implementam `IRequestHandler<TRequest, TResponse>`

**Exemplo**:
```csharp
public class GetDocumentosQueryHandler : IRequestHandler<GetDocumentosQuery, GetDocumentosResult>
{
    public async Task<GetDocumentosResult> Handle(GetDocumentosQuery request, CancellationToken ct)
    {
        // ...
    }
}
```

### Commands Should Have Names Ending With Command
```csharp
[Test]
public void Commands_Should_Have_Names_Ending_With_Command()
```

**Esperado**: `CreateDocumentoCommand`, `UpdateDocumentoCommand`

**Falha se**: `CreateDocumento` ou `DocumentoCreateCommand`.

### Queries Should Have Names Ending With Query
```csharp
[Test]
public void Queries_Should_Have_Names_Ending_With_Query()
```

**Esperado**: `GetDocumentosQuery`, `GetDocumentoByIdQuery`

**Falha se**: `GetDocumentos` ou `DocumentosGetQuery`.

## 🔒 Testes de Acoplamento

### DbContext Should Only Be Used In Infrastructure
```csharp
[Test]
public void DbContext_Should_Only_Be_Used_In_Infrastructure()
```

**Verifica**: Application não referencia `Microsoft.EntityFrameworkCore.DbContext`

**Falha se**: Encontra `DbContext` em Application.

### Controllers Should Only Be In Api Project
```csharp
[Test]
public void Controllers_Should_Only_Be_In_Api_Project()
```

**Verifica**: Application não contém `ControllerBase`

**Falha se**: Encontra Controllers em Application ou outros projetos.

### Repository Implementations Should Be In Infrastructure
```csharp
[Test]
public void Repository_Implementations_Should_Be_In_Infrastructure()
```

**Esperado**: `FiscalDocumentProcessor.Infrastructure.Repositories.DocumentoRepository`

**Falha se**: Repository concreto em Application.

### Service Implementations Should Be In Infrastructure Services
```csharp
[Test]
public void Service_Implementations_Should_Be_In_Infrastructure_Services()
```

**Esperado**: `FiscalDocumentProcessor.Infrastructure.Services.CompressionService`

**Falha se**: Serviço concreto em Application.

## 📐 Testes de Robustez

### Interfaces Should Be Prefixed With I
```csharp
[Test]
public void Interfaces_Should_Be_Prefixed_With_I()
```

**Esperado**: `IDocumentRepository`, `ICompressionService`

**Falha se**: `DocumentRepository` interface ou `DocumentRepositoryI`.

### Public Methods Should Not Be Async Void
```csharp
[Test]
public void Public_Methods_Should_Not_Be_Async_Void()
```

**Esperado**: `public async Task HandleAsync()`

**Falha se**: `public async void ProcessAsync()` (exceto event handlers)

## 📊 Interpretando Resultados

### Teste passou ✅
```
[PASS] Domain_Should_Not_Depend_On_Any_Other_Layer
```

Significa que o projeto está em conformidade com o requisito.

### Teste falhou ❌
```
[FAIL] Application_Should_Not_Depend_On_Infrastructure_Or_Api
  Reasons: [Domain types should not depend on other types]
  Failed types:
    - FiscalDocumentProcessor.Application.Features.Upload.UploadDocumentoCommand
```

**Causas comuns e soluções**:

| Erro | Causa | Solução |
|------|-------|---------|
| Application depende de Infrastructure | DbContext em Application | Mover para padrão Repository |
| Domain depende de Application | Logging em Entity | Usar domain events |
| Controllers em Application | Erro de estrutura | Mover para Api/Controllers |
| DTO com navigation property | Serialização complexa | Remover referência a Entity |

## 🔧 Ajustando Testes

### Afrouxar um teste (quando necessário)
```csharp
// ANTES: Muito restritivo
[Test]
public void Application_Should_Not_Depend_On_Any_CrossCutting()

// DEPOIS: Permitir especificamente logging
[Test]
public void Application_Should_Not_Depend_On_Except_Logging()
{
    var result = Types.InAssembly(ApplicationAssembly)
        .Should()
        .NotHaveDependencyOn("FiscalDocumentProcessor.Infrastructure")
        .And()
        .HaveDependencyOn("Serilog") // Permitido para logging
        .GetResult();
}
```

### Adicionar novo teste
```csharp
[Test]
public void MyCustomArchitectureRule()
{
    var result = Types.InAssembly(ApplicationAssembly)
        .That()
        .ResideInNamespace("FiscalDocumentProcessor.Application.MyFeature")
        .Should()
        .BeClasses()
        .GetResult();

    Assert.IsTrue(result.IsSuccessful, "Custom rule validation message");
}
```

## 🎯 Metas de Qualidade de Arquitetura

| Métrica | Alvo | Mínimo |
|---------|------|--------|
| Taxa de passar em tests arquitetura | 100% | 100% |
| Sem dependências circulares | Sim | Sim |
| Todas as interfaces com prefixo I | 100% | 95% |
| Repositories abstratos | 100% | 100% |
| DTOs sem navigation properties | 100% | 100% |

## 🚨 Troubleshooting

### Teste não encontra tipos
```
[FAIL] Domain_Should_Not_Depend_On_Any_Other_Layer
  No types found matching the criteria
```

**Solução**: Verificar namespace está correto
```csharp
var result = Types.InAssembly(DomainAssembly)
    // Usar InAssembly, não Assembly
```

### False positive em análise
```
[FAIL] Application_Should_Not_Depend_On_Infrastructure
  Type X depends on DbContext indirectly
```

**Solução**: Verificar referenced assemblies e indirect dependencies

### Teste muito lento
```bash
# Timeout ao rodar testes de arquitetura
```

**Solução**: NetArchTest faz análise via reflection, que é lenta
- Executar com `-c Release` (mais rápido)
- Rodar em paralelo: `dotnet test --filter "Category=Architecture" -p:ParallelizeTestCollections=true`

## 📚 Referências

- [NetArchTest.Rules Documentation](https://github.com/BenMorris/NetArchTest)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html)

---

**Última atualização**: 2026-02-15
**Autor**: FiscalDocumentProcessor Team
