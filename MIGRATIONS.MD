# Migration Guide

## ⚠️ Important: Database-Specific Migrations

This project uses **SQL Server for Development** and **PostgreSQL for Production**.
Always prefix migration names with `SqlServer_` or `PostgreSQL_` to avoid conflicts.

---

## For SQL Server (Development/Local)

### 1. Create a new migration (RECOMMENDED)

Use the automated script:

```powershell
.\add-migration.ps1 "YourFeatureName"
```

Or manually:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef migrations add SqlServer_YourFeatureName
```

### 2. Update your local database (RECOMMENDED)

Use the automated script to avoid PostgreSQL migration errors:

```powershell
.\update-local-db.ps1
```

This script automatically:

- Skips PostgreSQL migrations (marks them as applied without running)
- Applies only SQL Server migrations
- Provides clear status messages

### 3. Manual update (if needed)

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef database update
```

**Note:** If you get errors about PostgreSQL migrations, the script marks them as applied:

```powershell
# Manually mark a PostgreSQL migration as applied (don't run it)
sqlcmd -S "LUNARIA\SQLEXPRESS" -d "EnterpriseGradeInventoryDB" -E -Q "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20251214104614_PostgreSQL_InitialCreate', '9.0.0')"
```

---

## For PostgreSQL (Production)

### 1. Create a new migration

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
dotnet ef migrations add PostgreSQL_YourFeatureName
```

### 2. Update production database

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
dotnet ef database update
```

---

## Common Commands

### List all migrations

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef migrations list
```

### Remove last migration (if not applied)

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef migrations remove
```

### Generate SQL script

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet ef migrations script
```
