# Gram Shop POS — Backend

Production ASP.NET Core Web API for a **1 Gram Jewellery Shop** POS / billing / inventory system.

The future React frontend talks only to this REST API. It never connects to SQL Server.

```
React JS Frontend
        ↓
REST API (JWT)
        ↓
ASP.NET Core Web API
        ↓
Application services
        ↓
EF Core
        ↓
SQL Server (GramShopPOS)
```

This repository is **backend only**. There is no React, Angular, Blazor, MVC UI, or Razor.

## Architecture

| Project | Responsibility |
|---|---|
| `GramShopPOS.API` | Controllers, JWT, Swagger, CORS, middleware |
| `GramShopPOS.Application` | Business logic, DTOs, validators |
| `GramShopPOS.Domain` | Entities, enums, constants |
| `GramShopPOS.Infrastructure` | EF Core, SQL Server, Excel, PDF, seed |
| `GramShopPOS.Tests` | Unit + SQLite integration tests |

## Prerequisites

- .NET 9 SDK
- SQL Server 2019+ (LocalDB, Express, Developer, or Azure SQL)
- Windows, Linux, or Docker

## Configuration

`Backend/GramShopPOS.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=GramShopPOS;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "CHANGE_THIS_IN_PRODUCTION_TO_A_LONG_RANDOM_SECRET_KEY_64CHARS",
    "Issuer": "GramShopPOS",
    "Audience": "GramShopPOS.Client",
    "ExpiryMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

Production secrets must come from environment variables or a secret store:

```text
ConnectionStrings__DefaultConnection
Jwt__Key
Cors__AllowedOrigins__0
```

## Database setup

### Option A — EF Core (recommended)

```bash
cd Backend/GramShopPOS.API
dotnet ef database update --project ../GramShopPOS.Infrastructure
dotnet run
```

The Development host seeds roles, users, a sample store, categories, products, and settings.

### Option B — SQL scripts

```text
Database/01_CreateDatabase.sql
Database/02_Tables.sql
Database/03_Indexes.sql
Database/04_Constraints.sql
Database/05_SeedData.sql
Database/06_StoredProcedures.sql
```

Then start the API once so Identity-hashed users are created.

## Run

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project Backend/GramShopPOS.API
```

- HTTP: http://localhost:5088
- HTTPS: https://localhost:7088
- Swagger: http://localhost:5088/swagger
- Health: http://localhost:5088/health

## Default logins

| User | Password | Role |
|---|---|---|
| `admin` | `ChangeMe@123` | Admin |
| `salesperson` | `ChangeMe@123` | SalesPerson |

Both accounts require a password change on first login (`mustChangePassword`).

## Roles

**Admin** — all stores, pricing, users, settings, profit reports.

**SalesPerson** — assigned store(s) only. Cannot manage stores/users/global settings or view profit / current purchase prices. Store isolation is enforced on the server; a forged `storeId` returns **403**.

## Excel import

`GET /api/products/import/template` downloads the `.xlsx` template.

Columns: Product Code, Product Name, Category, Unit, Purchase Price, Selling Price, MRP, Tax %, Opening Stock, Store Code, Barcode.

Flow: `preview` → row-level errors → `confirm` (transactional upsert). Invalid files are not partially imported.

## Deployment

See [Documentation/DEPLOYMENT.md](Documentation/DEPLOYMENT.md) for IIS, Azure, and Docker.

## API / database docs

- [Documentation/API.md](Documentation/API.md)
- [Documentation/DATABASE.md](Documentation/DATABASE.md)
