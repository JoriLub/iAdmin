# iAdmin API Deployment (LoadIQ Server)

## Target
- Domain: `iadmin.api.loadiq.nl`
- Physical path: `\\serverweb\Data\WEB\PROD\LOADIQ.NL\IADMIN.API`

## 1. Server Prerequisites
- IIS installed with:
  - `Web Server`
  - `Static Content`
  - `ASP.NET Core Hosting Bundle (.NET 10)`
- DNS `iadmin.api.loadiq.nl` points to your server
- HTTPS certificate bound in IIS for `iadmin.api.loadiq.nl`

## 2. First-time IIS Setup
1. Create IIS Application Pool: `iAdmin.ApiPool`
- .NET CLR version: `No Managed Code`
- Pipeline mode: `Integrated`

2. Create IIS Website/Application:
- Site name: `iAdmin.Api`
- Physical path: `\\serverweb\Data\WEB\PROD\LOADIQ.NL\IADMIN.API`
- Binding: `https`, host name `iadmin.api.loadiq.nl`, port `443`
- Assign application pool: `iAdmin.ApiPool`

3. Grant file permissions to app pool identity on target path
- Read/Execute (and Modify if local sqlite DB in that path)

## 3. Production Config
File: `iAdmin.Server/appsettings.Production.json`

Set at least:
- `Jwt:SecretKey` to a strong random secret
- `ConnectionStrings:DefaultConnection` to your desired DB location
- `Cors:AllowedOrigins` to your real client origin(s)

## 4. Deploy Command
From `iAdmin.Server` run:

```powershell
.\Deploy-ToLoadIQ.ps1
```

This does:
1. `dotnet publish -c Release`
2. mirror copy to target path
3. prompt you to recycle IIS app pool/site

## 5. Validate Deployment
- Open: `https://iadmin.api.loadiq.nl/swagger` (if enabled in production)
- Test login endpoint:

```powershell
$body = @{ username = 'admin'; password = 'admin123' } | ConvertTo-Json
Invoke-RestMethod -Uri 'https://iadmin.api.loadiq.nl/api/auth/login' -Method Post -ContentType 'application/json' -Body $body
```

## 6. Recommended Hardening
- Move `Jwt:SecretKey` to environment variable on server
- Restrict `Cors:AllowedOrigins` to production frontend only
- Enable request logging and monitor logs
- Replace SQLite with SQL Server/PostgreSQL for multi-user production load
