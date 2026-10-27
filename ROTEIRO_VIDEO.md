# Roteiro do vídeo (≤ 4min30)

**Antes de gravar**
- `docker compose up -d` (container `healthy`)
- `dotnet run --project src/VetClinic.API` → abra http://localhost:5080/swagger
- Deixe abertos: o VS Code na raiz e um terminal na raiz do repositório
- Escolha um e-mail novo para a demo (ex.: `ana.demo1@vet.com`). Se repetir a gravação, troque o número.

---

## 0:00 – 0:35 · Estrutura da solution
- Mostre a árvore no VS Code: `src/` (Domain, Application, Infrastructure, API) e `tests/` (UnitTests, FunctionalTests).
- Fala: "Domain não referencia ninguém; Application depende do Domain; Infrastructure implementa as interfaces da Application; a API só orquestra."
- Abra rapidamente `Application/Common/OperationResult.cs` e `API/Controllers/BaseApiController.cs` (`ToActionResult`).

## 0:35 – 1:05 · Testes passando
```bash
dotnet test
```
- Destaque: **18 unitários + 15 funcionais, 0 falhas**. "Os funcionais rodam contra o SQL Server real do container; só o ViaCEP é mockado."

## 1:05 – 1:50 · Login + 401/403 no Swagger
1. Sem token: `GET /api/tutores` → **401**.
2. `POST /api/auth/login` como **user**:
```json
{ "username": "user", "password": "user123" }
```
3. **Authorize** → cole o token → `POST /api/tutores` com o JSON do passo seguinte → **403** (User não escreve).
4. **Logout** no Authorize → login como **admin**:
```json
{ "username": "admin", "password": "admin123" }
```
5. **Authorize** com o token do admin.

## 1:50 – 2:25 · Criar tutor (ViaCEP preenchendo o endereço)
`POST /api/tutores`:
```json
{ "nome": "Ana Souza", "email": "ana.demo1@vet.com", "cep": "01001-000" }
```
- **201**: mostre `logradouro: "Praça da Sé"`, `bairro`, `cidade`, `uf` preenchidos pelo ViaCEP e os `links` (self, update, delete, pets).
- Crie um segundo tutor para a paginação:
```json
{ "nome": "Bruno Lima", "email": "bruno.demo1@vet.com", "cep": "20040-020" }
```
- (opcional) `POST /api/pets`, trocando o `tutorId` pelo id retornado:
```json
{ "nome": "Rex", "especie": "Cachorro", "raca": "Vira-lata", "dataNascimento": "2020-05-10", "tutorId": 1 }
```

## 2:25 – 3:05 · Listagem paginada (PageResult + HATEOAS)
`GET /api/tutores?pageNumber=1&pageSize=1`
- Mostre `pageNumber`, `pageSize`, `totalItems`, `totalPages`, `hasPrevious`, `hasNext`.
- Mostre os `links` da página: `self`, `first`, `last`, `next`. Os links de cada item ficam dentro de `items`.
- Abra `Infrastructure/Repositories/TutorRepository.cs` e mostre os comentários `// OTIMIZACAO`: AsNoTracking, `Skip/Take` + `CountAsync` no banco, `Select` direto para o DTO.

## 3:05 – 3:50 · Erros customizados
**Retorno (OperationResult → 404):** `GET /api/tutores/999999`
- ProblemDetails com `code: "Tutor.NaoEncontrado"`.
- Mostre `TutorRepository.GetByIdAsync` → `// ERRO CUSTOMIZADO - RETORNO`.

**Lançamento (exception → middleware → 409):** repita o `POST /api/tutores` com o mesmo e-mail:
```json
{ "nome": "Ana Souza", "email": "ana.demo1@vet.com", "cep": "01001-000" }
```
- **409** com `code: "Tutor.EmailDuplicado"`.
- Mostre `TutorRepository.EnsureEmailIsUniqueAsync` → `// ERRO CUSTOMIZADO - LANÇAMENTO` e o `ExceptionHandlingMiddleware`.

## 3:50 – 4:25 · ViaCEP isolado + resiliência
1. `GET /api/cep/01001000` → **200** com o endereço (repita: a segunda chamada vem do OutputCache).
2. `GET /api/cep/123` → **400** `Cep.Invalido`.
3. `GET /api/cep/99999999` → **404** `Cep.NaoEncontrado`.
- Mostre `Infrastructure/DependencyInjection.cs` (`AddHttpClient` + `AddStandardResilienceHandler`) e o `try/catch` de `ViaCepService`, que devolve **503** se o ViaCEP cair, sem derrubar a API.

## 4:25 – 4:30 · Encerramento
"Clean Architecture, Operation Result, erros por retorno e por lançamento, ViaCEP resiliente, PageResult com HATEOAS, JWT com roles e testes passando contra o SQL Server real."
