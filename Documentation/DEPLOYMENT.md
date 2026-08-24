# Deployment

## Production configuration

Set these as environment variables. Do not commit secrets.

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=...;Database=GramShopPOS;User Id=...;Password=...;TrustServerCertificate=True;Encrypt=True;
Jwt__Key=<64+ character random key>
Jwt__Issuer=GramShopPOS
Jwt__Audience=GramShopPOS.Client
Cors__AllowedOrigins__0=https://your-frontend.example
```

- HTTPS is required in Production (`UseHttpsRedirection`).
- Swagger is disabled when `ASPNETCORE_ENVIRONMENT=Production`.
- CORS never uses `AllowAnyOrigin`.

## Database migration

On the server:

```bash
dotnet ef database update --project Backend/GramShopPOS.Infrastructure --startup-project Backend/GramShopPOS.API
```

Or run `Database/01_CreateDatabase.sql` then `02_Tables.sql` … `06_StoredProcedures.sql`.

Then start the API once (or with `--seed`) so hashed users exist.

## IIS / Windows Server

1. Install .NET 9 Hosting Bundle.
2. `dotnet publish Backend/GramShopPOS.API -c Release -o C:\inetpub\GramShopPOS`.
3. Create an IIS site pointing at the publish folder, no managed code, HTTPS binding.
4. Set environment variables on the app pool or `web.config`.
5. Grant the app pool identity access to SQL Server.
6. Logs write to `logs/gramshop-*.log`.

## Azure

- App Service (Windows or Linux) with .NET 9.
- Azure SQL with a contained user or managed identity.
- Store `Jwt__Key` in App Settings / Key Vault.
- Configure CORS to the Static Web Apps / App Service frontend origin.
- Enable HTTPS only.

## Docker

```bash
docker build -t gramshop-pos-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Jwt__Key="..." \
  -e Cors__AllowedOrigins__0="https://frontend" \
  gramshop-pos-api
```

Apply migrations as a release step before or during container start.

## Logging

Serilog writes to console and rolling files. Do not log passwords, JWT tokens, or connection strings. Production error responses never include stack traces.
