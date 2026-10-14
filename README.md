# Partners API — clientes e fornecedores

API ASP.NET Core para gerir os clientes e fornecedores de uma empresa. Cada entidade pode ser `CUSTOMER`,
`SUPPLIER` ou `BOTH`, e existe um único registo por NIF.

## Versões

| Componente | Versão |
|---|---|
| .NET SDK | 10.0.402 (`global.json` aceita qualquer SDK 10.0.1xx ou superior) |
| Runtime / ASP.NET Core | .NET 10 (LTS) |
| EF Core + provider SqlServer + dotnet-ef | 10.0.13 |
| SQL Server | 2022, edição Developer (imagem `mcr.microsoft.com/mssql/server:2022-latest`) |
| Testes | xUnit 2.9.3, Microsoft.AspNetCore.Mvc.Testing 10.0.13, Testcontainers.MsSql 4.16.0 |
| OpenAPI / Swagger UI | Swashbuckle.AspNetCore 10.3.0 |

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (para o SQL Server de desenvolvimento e para os testes de integração), **ou** um SQL Server
  2019+ próprio (Developer e Express são gratuitos — ver nota em [Sem Docker](#sem-docker)).

## Preparação

```bash
docker compose up -d --wait     # SQL Server em localhost,14333 (sa / Partners#Dev2026)
dotnet tool restore             # instala dotnet-ef a partir do manifesto local (dotnet-tools.json)
dotnet ef database update -p src/Partners.Infrastructure -s src/Partners.Infrastructure
```

O último comando aplica a migração `InitialSchema`. Em `Development`, a API também aplica migrações
pendentes no arranque e insere 6 parceiros de exemplo se a tabela estiver vazia
(`Database:MigrateOnStartup` e `Database:SeedSampleData` em `appsettings.Development.json`).

## Execução

```bash
dotnet run --project src/Partners.Api        # http://localhost:5080
dotnet watch --project src/Partners.Api      # com hot reload
```

- Swagger UI: http://localhost:5080/swagger
- Exemplos de pedidos: [http/partners.http](http/partners.http) (REST Client do VS Code, Visual Studio ou Rider).

## Testes

```bash
dotnet build
dotnet test
```

- **Partners.UnitTests** — regras de domínio (normalização e validação de Name, NIF, Email, CreditLimit,
  Type), critérios de pesquisa e token de versão. Não precisam de base de dados.
- **Partners.IntegrationTests** — a API completa (`WebApplicationFactory`) sobre **SQL Server real**.
  Por omissão o Testcontainers arranca um contentor SQL Server 2022 (requer Docker). Cada execução cria uma
  base de dados nova através das migrações EF Core.

Para usar um SQL Server existente em vez do Docker, defina a connection string do servidor (é criada e
apagada uma base `PartnersTests_<guid>`):

```bash
# bash
export PARTNERS_TEST_SQLSERVER="Server=localhost;User Id=sa;Password=...;TrustServerCertificate=True"
# PowerShell
$env:PARTNERS_TEST_SQLSERVER = "Server=localhost;User Id=sa;Password=...;TrustServerCertificate=True"
dotnet test
```

### Cenários do enunciado

| # | Cenário | Testes |
|---|---|---|
| 1 | Normalização de Name/NIF, 409 com outro formato, NIF reservado após desativação | `Scenario1_NormalizationAndUniquenessTests` |
| 2 | `BOTH` em todos os filtros; `CUSTOMER` fora de `SUPPLIER` | `ListPartnersTests.Scenario2_*` |
| 3 | Desativar/reativar por PUT, `IncludeInactive`, GET por Id | `ListPartnersTests.Scenario3_*` |
| 4 | Manter o próprio NIF vs. NIF de outra entidade (409 sem alterações) | `UpdatePartnerTests.Scenario4_*` |
| 5 | POST simultâneos com o mesmo NIF; PUT simultâneos com a mesma Version | `Scenario5_ConcurrencyTests` |
| 6 | CreditLimit=10,999, NIF com letras, Page=0, Type inválido, Version malformada — sem alterações | `Scenario6_InvalidRequestTests` |
| 7 | Filtros combinados, TotalCount antes da paginação, desempate por Id, `Search=Ana` | `ListPartnersTests.Scenario7_*` |

## Endpoints

| Método | Rota | Sucesso | Erros |
|---|---|---|---|
| `POST` | `/api/partners` | 201 + `Location` | 400, 409 (NIF duplicado) |
| `GET` | `/api/partners/{id}` | 200 (também inativos) | 400 (Id malformado), 404 |
| `GET` | `/api/partners` | 200 `{ items, totalCount, page, pageSize }` | 400 |
| `PUT` | `/api/partners/{id}` | 200 com nova `version` | 400, 404, 409 (NIF duplicado ou versão desatualizada) |

Parâmetros da listagem: `search`, `type`, `includeInactive` (`false`), `page` (`1`, ≥ 1), `pageSize`
(`20`, 1–100). Os nomes JSON e de query não distinguem maiúsculas (`Search` = `search`); as respostas
usam camelCase.

O `PUT` é uma substituição completa: `name`, `taxNumber`, `type`, `email`, `creditLimit`, `isActive` e
`version` têm de estar presentes no JSON (`email` pode ser `null`). `id`, `createdAt` e `updatedAt` não são
editáveis e são ignorados se enviados.

Todos os erros são `application/problem+json` (RFC 9457) com `title`, `status`, `detail`, `traceId` e,
nos 400, `errors` por campo — sem stack traces:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "errors": { "CreditLimit": ["CreditLimit must have at most 2 decimal places."] },
  "traceId": "0HNP9VDPO1U1T:00000001"
}
```

## Regras implementadas

- **Name**: obrigatório, `Trim`, 2–100 caracteres. Pesquisa insensível a maiúsculas (colação
  `Latin1_General_100_CI_AS` na coluna, independente da colação da base).
- **TaxNumber**: remove espaços, pontos e hífenes; exige exatamente 9 dígitos ASCII; guardado como
  `char(9)` (preserva zeros iniciais). Regra simplificada, sem dígito de controlo fiscal.
- **Email**: vazio/espaços → `null`; senão `Trim` e validação de formato.
- **CreditLimit**: 0 a 1 000 000, no máximo 2 casas decimais (`10.990` é aceite, `10.999` é rejeitado —
  nunca arredonda), `decimal(18,2)`. Números enviados como string JSON são rejeitados.
- **Type**: exatamente `CUSTOMER`, `SUPPLIER` ou `BOTH`. No filtro, `CUSTOMER` e `SUPPLIER` incluem `BOTH`.
- **IsActive** começa a `true`; **CreatedAt/UpdatedAt** em UTC definidos pelo servidor; `CreatedAt` nunca muda.

### Unicidade do NIF

Garantida pelo índice único `UX_Partners_TaxNumber` no SQL Server (abrange inativos). A API não faz
pré-verificação nem usa locks: tenta gravar e traduz o erro de chave duplicada (2601/2627) para 409.
Pedidos concorrentes são, portanto, serializados pela própria base de dados. Check constraints na tabela
repetem as regras de NIF, Type e CreditLimit como defesa em profundidade.

### Concorrência otimista

`Version` é a coluna `rowversion` do SQL Server, exposta como Base64 (8 bytes). O `PUT` gera
`UPDATE ... WHERE Id = @id AND Version = @versionDoPedido`; se outra escrita mudou a linha entretanto,
nenhuma linha é afetada, o EF Core lança `DbUpdateConcurrencyException` e a API devolve 409. A comparação
é feita pelo SQL Server no momento da escrita, e não em C# (há um teste que altera a linha entre a leitura
e a escrita para o demonstrar). Todas as validações acontecem antes de qualquer escrita e a atualização é
um único `UPDATE`, logo um pedido inválido nunca altera parcialmente o registo.

### Pesquisa

`search` faz correspondência parcial no nome **ou** no NIF. Para o NIF, a pesquisa é normalizada com os
mesmos separadores; o ramo do NIF só é usado se o resultado for uma sequência não vazia de dígitos — assim
`search=Ana` ou `search=---` nunca fazem corresponder todos os NIF. Os caracteres `%`, `_` e `[` são
escapados (literais). Filtros, `COUNT(*)` (antes da paginação), `ORDER BY Name, Id` e `OFFSET/FETCH`
executam no SQL Server; se a página pedida estiver além dos resultados, a segunda query é evitada.

## Arquitetura

```
src/
  Partners.Domain          Entidade Partner, value objects (PartnerDetails, TaxNumber), PartnerType,
                           exceções de domínio, interfaces IPartnerRepository / IPartnerQueryGateway
  Partners.Application     Casos de uso (Create/Get/Update/List), contratos de entrada/saída, VersionToken
  Partners.Infrastructure  EF Core: PartnersDbContext, PartnerModel + mapeamento, migrações,
                           SqlServerPartnerRepository, SqlServerPartnerQueryGateway, dados de exemplo
  Partners.Api             Controller, DTOs de pedido, tratamento de erros (ProblemDetails), Swagger
tests/
  Partners.UnitTests
  Partners.IntegrationTests
http/partners.http
```

Domain e Application não dependem de EF Core nem de ASP.NET Core. A entidade de domínio não tem setters
públicos: é criada por `Partner.Create`, alterada por `Update` e reconstruída a partir da base por
`Partner.From`; a infraestrutura mapeia-a explicitamente para `PartnerModel`.

## Sem Docker

1. Instale SQL Server Developer ou Express: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Aponte a API para ele, por exemplo:
   ```bash
   export ConnectionStrings__Partners="Server=localhost\\SQLEXPRESS;Database=Partners;Trusted_Connection=True;TrustServerCertificate=True"
   ```
   (o mesmo valor é usado por `dotnet ef database update`).
3. Para os testes, defina `PARTNERS_TEST_SQLSERVER` como indicado em [Testes](#testes).
