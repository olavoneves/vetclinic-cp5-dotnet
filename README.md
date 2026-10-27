# VetClinic API — CP5 .NET (FIAP)

API RESTful de uma **Clínica Veterinária** em **ASP.NET Core 8 (Controllers)** seguindo **Clean Architecture**.
Ela cadastra **Tutores** e seus **Pets** (relação 1:N), preenche o endereço do tutor pelo **ViaCEP**, protege
os endpoints com **JWT (roles Admin/User)**, devolve listas paginadas com **PageResult + HATEOAS** e padroniza
os erros com **Operation Result**, **exceptions de domínio** e **ProblemDetails**.

**Link do vídeo:** https://youtu.be/nYg1bPDoiA4

---

## Arquitetura

```
                ┌──────────────────────────────────────────────┐
                │ VetClinic.API                                │
                │ Controllers · Middleware de erro · JWT       │
                │ HATEOAS · Swagger · OutputCache · Program.cs │
                └───────────────┬───────────────┬──────────────┘
                                │               │
                                ▼               ▼
┌───────────────────────────────────┐   ┌──────────────────────────────────┐
│ VetClinic.Application             │◄──│ VetClinic.Infrastructure         │
│ Services · DTOs · OperationResult │   │ DbContext · Migrations           │
│ PageResult · Interfaces           │   │ Repositórios · ViaCepService     │
│ (ICepService, I*Repository)       │   │ (HttpClient + Resilience)        │
└─────────────────┬─────────────────┘   └────────────────┬─────────────────┘
                  │                                      │
                  ▼                                      ▼
                ┌──────────────────────────────────────────────┐
                │ VetClinic.Domain                             │
                │ Entidades (Tutor, Pet) · Exceptions de domínio│
                │ (não referencia nenhum outro projeto)        │
                └──────────────────────────────────────────────┘
```

Dependências: **API → Application/Infrastructure**, **Infrastructure → Application/Domain**,
**Application → Domain**, **Domain → nada**.

> **Decisão de projeto:** as interfaces de repositório (`ITutorRepository`, `IPetRepository`) ficam em
> `Application/Interfaces`, e não em `Domain`, porque o requisito pede que o repositório **retorne
> `OperationResult<T>`**, que pertence à Application. Se elas ficassem no Domain, ele teria que referenciar
> a Application, o que quebraria a regra de dependências.

```
VetClinic.sln
├── src/
│   ├── VetClinic.Domain           Entities/, Errors/
│   ├── VetClinic.Application      Common/ (OperationResult, PageResult, Error), DTOs/, Interfaces/, Services/
│   ├── VetClinic.Infrastructure   Persistence/ (DbContext + Migrations), Repositories/, ExternalServices/ (ViaCEP)
│   └── VetClinic.API              Controllers/, Middleware/, Hateoas/, Auth/, Common/, Program.cs
└── tests/
    ├── VetClinic.UnitTests        xUnit + Moq (TutorService, PetService)
    └── VetClinic.FunctionalTests  xUnit + WebApplicationFactory (SQL Server REAL do container)
```

## Tecnologias

- .NET 8 / ASP.NET Core Web API (Controllers)
- Entity Framework Core 8 + SQL Server 2022 (Docker)
- JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- `HttpClient` tipado + `Microsoft.Extensions.Http.Resilience` (ViaCEP)
- Swagger (Swashbuckle) com botão **Authorize**
- xUnit, Moq e `Microsoft.AspNetCore.Mvc.Testing`

## Pré-requisitos

- **.NET 8 SDK** (ou um SDK mais novo com o runtime 8.0 instalado; o projeto compila para `net8.0`)
- **Docker** com Docker Compose
- (opcional) `dotnet-ef`: `dotnet tool install -g dotnet-ef --version 8.*`

## Como rodar

```bash
# 1. Variáveis do container (senha do SA)
cp .env.example .env

# 2. Configuração local da API (connection string + chave JWT) — arquivo ignorado pelo git
cp src/VetClinic.API/appsettings.example.json src/VetClinic.API/appsettings.Development.json

# 3. Sobe o SQL Server (porta 14333) e espera ficar "healthy"
docker compose up -d
docker inspect -f '{{.State.Health.Status}}' vetclinic-sqlserver   # → healthy

# 4. Roda a API (as migrations são aplicadas automaticamente no startup)
dotnet run --project src/VetClinic.API
# Swagger: http://localhost:5080/swagger

# 5. Testes (com o container de pé)
dotnet test
```

### Banco de dados

| Item | Valor |
|---|---|
| Container | `vetclinic-sqlserver` (`mcr.microsoft.com/mssql/server:2022-latest`) |
| Porta | **14333** → 1433 (a 1433 fica livre para outras ferramentas) |
| Volume | `vetclinic-sqldata` |
| Senha SA | `MSSQL_SA_PASSWORD` no `.env` |
| Connection string | `Server=localhost,14333;Database=VetClinicDb;User Id=sa;Password=<da .env>;TrustServerCertificate=True` |

A connection string pode vir do `appsettings.Development.json` **ou** da variável de ambiente
`ConnectionStrings__Default`.

**Migrations:** o `Program.cs` chama `Database.Migrate()` no startup. Para aplicar manualmente:

```bash
dotnet ef database update -p src/VetClinic.Infrastructure -s src/VetClinic.API
# Nova migration:
dotnet ef migrations add NomeDaMigration -p src/VetClinic.Infrastructure -s src/VetClinic.API -o Persistence/Migrations
```

### Configuração do JWT

| Chave | Onde | Observação |
|---|---|---|
| `Jwt:Key` | `appsettings.Development.json` ou env `Jwt__Key` | **mínimo 32 caracteres**; a API não sobe sem ela |
| `Jwt:Issuer` / `Jwt:Audience` | `appsettings.json` | `VetClinic.API` / `VetClinic.Clients` |
| `Jwt:ExpiresMinutes` | `appsettings.json` | padrão 60 |
| `Auth:Users` | `appsettings.json` | usuários fixos (não há tabela de usuários) |

### Configuração do ViaCEP

- `BaseAddress`: `https://viacep.com.br/ws/` (em `Infrastructure/DependencyInjection.cs`)
- `AddStandardResilienceHandler()`: retry, circuit breaker, timeout de 4s por tentativa e de 12s no total
- Falhas viram `OperationResult`: CEP mal formatado → **400**, CEP inexistente → **404**, ViaCEP fora/timeout → **503**
- Nos testes funcionais o `ICepService` é substituído por um mock (os testes não dependem da internet)

## Credenciais de teste

| Usuário | Senha | Role |
|---|---|---|
| `admin` | `admin123` | Admin (leitura e escrita) |
| `user` | `user123` | User (somente leitura) |

No Swagger: `POST /api/auth/login`, copie o `token` → botão **Authorize** → cole o token (sem "Bearer").

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| POST | `/api/auth/login` | Anônimo | Gera o token JWT |
| GET | `/api/tutores?pageNumber=1&pageSize=10` | Logado | Lista paginada + HATEOAS |
| GET | `/api/tutores/{id}` | Logado | Busca por id (404 customizado) |
| POST | `/api/tutores` | **Admin** | Cria (endereço via ViaCEP; 409 se e-mail duplicado) |
| PUT | `/api/tutores/{id}` | **Admin** | Atualiza |
| DELETE | `/api/tutores/{id}` | **Admin** | Remove (pets em cascata) |
| GET | `/api/pets?pageNumber=1&pageSize=10&tutorId=` | Logado | Lista paginada (filtro opcional por tutor) |
| GET | `/api/pets/{id}` | Logado | Busca por id |
| POST | `/api/pets` | **Admin** | Cria (404 se o tutor não existir; 422 se a data de nascimento for futura) |
| PUT | `/api/pets/{id}` | **Admin** | Atualiza |
| DELETE | `/api/pets/{id}` | **Admin** | Remove |
| GET | `/api/cep/{cep}` | Anônimo | Consulta isolada ao ViaCEP (com OutputCache) |

`pageSize` máximo: **50**. Sem token → **401**; com role `User` em POST/PUT/DELETE → **403**.

## Onde está cada requisito

| # | Requisito | Arquivos |
|---|---|---|
| 1 | **Operation Result** | `src/VetClinic.Application/Common/OperationResult.cs`, `Common/Error.cs`; conversão única em `src/VetClinic.API/Controllers/BaseApiController.cs` (`ToActionResult`) |
| 2 | **Erros customizados — RETORNO** | `src/VetClinic.Infrastructure/Repositories/TutorRepository.cs` e `PetRepository.cs` → `GetByIdAsync` (busque `// ERRO CUSTOMIZADO - RETORNO`) |
| 2 | **Erros customizados — LANÇAMENTO** | `TutorRepository.EnsureEmailIsUniqueAsync` (ConflictException), `PetRepository.EnsureTutorExistsAsync` (NotFoundException), `Application/Services/PetService.ValidateBirthDate` (BusinessRuleException) — busque `// ERRO CUSTOMIZADO - LANÇAMENTO` |
| 2 | Exceptions de domínio | `src/VetClinic.Domain/Errors/*.cs` |
| 2 | Middleware global | `src/VetClinic.API/Middleware/ExceptionHandlingMiddleware.cs` (404/409/422/400/500 em ProblemDetails) + `API/Common/ProblemFactory.cs` |
| 3 | **ViaCEP** | `Application/Interfaces/ICepService.cs`, `Infrastructure/ExternalServices/ViaCepService.cs`, resiliência em `Infrastructure/DependencyInjection.cs`, uso em `Application/Services/TutorService.CreateAsync`, endpoint `API/Controllers/CepController.cs` |
| 4 | **PageResult** | `src/VetClinic.Application/Common/PageResult.cs` |
| 4 | **HATEOAS** | `src/VetClinic.API/Hateoas/HateoasHelper.cs` (`Url.Link` + rotas nomeadas), `Application/DTOs/LinkDto.cs` |
| 5 | **Otimização** | busque `// OTIMIZACAO`: `Infrastructure/Repositories/*.cs` (AsNoTracking, Select→DTO, Skip/Take + CountAsync), `Infrastructure/Persistence/VetClinicDbContext.cs` (índices), `API/Controllers/CepController.cs` (OutputCache) |
| 6 | **JWT / Roles** | `API/Program.cs`, `API/Auth/TokenService.cs`, `API/Controllers/AuthController.cs`, atributos `[Authorize]` nos controllers |
| 7 | **CRUD + validação** | `API/Controllers/TutoresController.cs`, `PetsController.cs`; DataAnnotations em `Application/DTOs/*.cs` |
| 8 | **Testes unitários** | `tests/VetClinic.UnitTests/TutorServiceTests.cs`, `PetServiceTests.cs` |
| 8 | **Testes funcionais** | `tests/VetClinic.FunctionalTests/*.cs` (factory em `Infrastructure/VetClinicApiFactory.cs`) |
| 9 | **Migrations** | `src/VetClinic.Infrastructure/Persistence/Migrations/`, `Database.Migrate()` em `API/Program.cs` |

## Testes

```bash
docker compose up -d   # o SQL Server precisa estar de pé
dotnet test
```

- **Unitários (18):** sucesso, NotFound, Conflict, regra de negócio, falha do ViaCEP mockada, normalização da paginação.
- **Funcionais (15):** login ok/falha, 401 sem token, 403 com User, criar tutor (201 + links HATEOAS),
  listagem paginada (metadados + links), 404 customizado, 409 e-mail duplicado, 503 ViaCEP fora (mock),
  pet com tutor inexistente (404 lançado), 422 regra de negócio, fluxo PUT/DELETE.
- Os funcionais usam o **mesmo SQL Server do container** (nada de SQLite/InMemory). Cada teste cria
  dados com e-mail único (Guid) e apaga tudo no final. A senha vem de `MSSQL_SA_PASSWORD`, do `.env`
  ou do valor do `.env.example`; `ConnectionStrings__Default` sobrescreve tudo.

## Segurança

`.env` e `appsettings.Development.json` estão no `.gitignore`. Só são versionados `.env.example` e
`appsettings.example.json`, que contêm valores de exemplo, não segredos reais.
