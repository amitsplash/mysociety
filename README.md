# MySociety MVP — Backend

Mobile-first SaaS for recurring group contributions, expense approvals, and immutable member ledgers.

## Status: Backend MVP complete

- Clean Architecture (.NET 8, EF Core, SQLite)
- Full REST API with Swagger
- JWT authentication
- Ledger engine (immutable entries, balance derived)
- Group / Member / Contribution / Payment / Expense / Ledger APIs
- Development seed data
- **22 unit tests** passing

## Run

```bash
dotnet build MySociety.sln
dotnet test MySociety.sln
dotnet run --project src/Api/MySociety.Api.csproj --launch-profile http
```

- Swagger: http://localhost:5221/swagger
- Health: http://localhost:5221/api/health

On first run in **Development**, migrations apply and demo data is seeded.

## Demo login (after seed)

| Phone       | Password      | Role        | Member ID (use as `X-Member-Id`) |
|-------------|---------------|-------------|----------------------------------|
| 9000000001  | Password123!  | SuperAdmin  | `33333333-3333-3333-3333-333333333301` |
| 9000000002  | Password123!  | Member      | `33333333-3333-3333-3333-333333333302` |
| 9000000003  | Password123!  | Member      | `33333333-3333-3333-3333-333333333303` |

## Authentication

1. `POST /api/auth/login` with `{ "phone": "9000000001", "password": "Password123!" }`
2. Copy `token` from response
3. In Swagger: **Authorize** → `Bearer {token}`
4. For protected calls, also set header **`X-Member-Id`** to the membership `memberId` for the group you are acting in

## API summary

| Area | Endpoints |
|------|-----------|
| Auth | `POST /api/auth/login` |
| Groups | `POST/GET/PUT /api/groups` |
| Members | `POST/PUT/DELETE /api/members`, `GET /api/groups/{id}/members` |
| Contributions | `POST /api/contributions/generate`, `GET /api/members/{id}/contributions` |
| Payments | `POST /api/payments` |
| Expenses | `POST /api/expenses`, `PATCH .../approve`, `PATCH .../reject`, `GET /api/groups/{id}/expenses` |
| Ledger | `GET /api/ledger/{memberId}`, `GET /api/groups/{groupId}/balances` |

`POST /api/groups` remains **anonymous** (bootstrap new societies). All other business endpoints require JWT + `X-Member-Id`.

## Solution layout

```text
src/Api              — HTTP, Swagger, JWT
src/Application      — Services, DTOs, validators
src/Domain           — Entities, enums
src/Infrastructure   — EF Core, repositories, ledger, seed
tests/Application.Tests
```

## Configuration

`src/Api/appsettings.json`:

- `ConnectionStrings:DefaultConnection` — SQLite file (`mysociety.db`)
- `Jwt:*` — signing key (change in production)

## Mobile app

Expo React Native app in `mobile/`:

```bash
cd mobile && npm install && npm start
```

See [mobile/README.md](mobile/README.md) for API URL setup (emulator vs physical device).
