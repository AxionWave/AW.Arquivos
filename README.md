# AW.Arquivos

API compartilhada de arquivos da Enterprise X. Qualquer front e qualquer API da empresa pode enviar, baixar, atualizar e desativar arquivos.

.NET 8, camadas:

```
src/
  Arquivos.API            → HTTP, JWT, token interno, controllers
  Arquivos.Application    → casos de uso
  Arquivos.Infrastructure → EF Core (schema `arquivos`), disco local, auth
  Arquivos.Core           → entidade, claims Enterprise, token `arq_*`
```

Não duplica `usuarios`. O JWT é o **mesmo** emitido pelo oAuth (via Gateway). APIs internas podem chamar direto com `X-Internal-Service-Token`.

## Comportamento

- Cada `POST` de upload **sempre** cria um registro novo e devolve um **token único** (`arq_…`), mesmo que o arquivo seja binariamente idêntico a outro já enviado.
- O token é o identificador público (metadados, download, update, desativar).
- Isolamento por `empresaId` (multi-tenant). SuperAdmin pode informar `empresaId`.
- Desativar é lógico: o registro permanece; o download passa a responder **410**.
- Conteúdo no disco; metadados no Postgres (nome, tamanho, MIME, SHA-256, origem, etc.). O hash é só integridade — **não** é usado para deduplicar.

## Integração Enterprise

| Peça | Valor |
|------|--------|
| Sistema (`core.sistemas.codigo`) | `ARQ` |
| Módulo de segurança | `ARQUIVOS000000` |
| Gateway | `/api/arquivos/**` |
| Service id | `arquivos` |
| Porta local | `8093` |
| Schema Postgres | `arquivos` |

O CRUD **não** exige o módulo `ARQUIVOS000000` no JWT — qualquer usuário autenticado da empresa pode usar. Os módulos existem para catálogo ASC / administração.

Fronts: Gateway (`http://localhost:8080`) + `X-Secret-Token` + Bearer.  
Outras APIs: `http://localhost:8093` (ou `http://arquivos:8080` na rede Docker) + `X-Internal-Service-Token` + `X-Empresa-Id`.

## Endpoints

Todos (exceto `/health`) exigem JWT **ou** `X-Internal-Service-Token`.

| Método | Path | Descrição |
|--------|------|-----------|
| GET | `/health` | Healthcheck |
| GET | `/api/arquivos/me` | Claims do chamador |
| POST | `/api/arquivos` | Upload (`multipart/form-data`, campo `file`) → `{ token, … }` |
| GET | `/api/arquivos` | Lista paginada da empresa |
| GET | `/api/arquivos/{token}` | Metadados |
| GET | `/api/arquivos/{token}/download` | Bytes (`?inline=true` para embutir) |
| PUT | `/api/arquivos/{token}` | Substitui conteúdo e/ou metadados (token não muda) |
| PATCH | `/api/arquivos/{token}/desativar` | Soft-disable |
| PATCH | `/api/arquivos/{token}/ativar` | Reativa |

### POST upload (form)

| Campo | Obrigatório | Notas |
|-------|-------------|--------|
| `file` | sim | Qualquer tipo |
| `descricao` | não | Texto curto |
| `sistemaOrigem` | não | Ex.: `ASC`, `ORI`, `LYR` (ou header `X-Origin-System`) |
| `moduloOrigem` | não | Código do módulo |
| `referenciaExterna` | não | ID no sistema de origem |
| `metadados` | não | JSON objeto `string → string` |
| `empresaId` | SuperAdmin / chamada interna sem claim | Tenant |

Resposta `201`:

```json
{
  "token": "arq_…",
  "nomeOriginal": "contrato.pdf",
  "contentType": "application/pdf",
  "tamanhoBytes": 128000,
  "checksumSha256": "…",
  "empresaId": 1,
  "ativo": true,
  "dataCriacao": "2026-08-31T00:00:00+00:00"
}
```

### Chamada interna (API → API)

```http
POST http://localhost:8093/api/arquivos
X-Internal-Service-Token: <GATEWAY_INTERNAL_TOKEN>
X-Empresa-Id: 1
X-Origin-System: ORI
X-User-Id: 42
```

## Pré-requisitos

- .NET 8 SDK
- Gateway + oAuth + Core + Postgres no ar
- Seed: `infra/seed-sistema.sql`
- Schema: `infra/create-schema.sql` (a API também cria o schema no boot)
- `JWT_SECRET` **igual** ao oAuth/Gateway
- `GATEWAY_INTERNAL_TOKEN` igual ao Gateway/Core (chamadas service-to-service)

## Rodar local

```bash
cd AW.Arquivos
dotnet restore
dotnet run --project src/Arquivos.API
# http://localhost:8093/health
# Swagger: http://localhost:8093/swagger
```

Gateway local (sem Eureka):

```
GATEWAY_ARQUIVOS_URI=http://localhost:8093
```

Smoke (depois do login):

```http
POST http://localhost:8080/api/arquivos
Authorization: Bearer <access_token>
X-Secret-Token: <FRONTEND_SECRET_TOKEN>
Content-Type: multipart/form-data

file=@documento.pdf
sistemaOrigem=ASC
```

Limite padrão: **50 MB** (`Storage:MaxFileBytes`). Ajuste também o Nginx (`client_max_body_size`) em produção.

## O que NÃO fazer

- Novo login / tabela de usuários
- Chamar Core `:8081` do browser
- Tratar o SHA-256 como chave de deduplicação
- Reutilizar o token de um POST anterior
