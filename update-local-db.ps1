# Apply Migrations for SQL Server (Development) Only
# This script automatically skips PostgreSQL migrations

Write-Host "Setting up SQL Server Development Database..." -ForegroundColor Cyan

# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT='Development'

# Get list of all migrations
Write-Host "`nChecking migrations..." -ForegroundColor Yellow
$migrationsOutput = dotnet ef migrations list 2>&1 | Out-String

# Extract migration names
$allMigrations = $migrationsOutput -split "`n" | Where-Object { $_ -match '^\d{14}_' }

# Get pending migrations
$pendingMigrations = $allMigrations | Where-Object { $_ -match '\(Pending\)' }

if ($pendingMigrations.Count -eq 0) {
    Write-Host "`nNo pending migrations. Database is up to date!" -ForegroundColor Green
    exit 0
}

Write-Host "`nPending migrations found:" -ForegroundColor Yellow
$pendingMigrations | ForEach-Object { Write-Host "  $_" }

# Find PostgreSQL migrations in pending list
$postgresqlMigrations = $pendingMigrations | Where-Object { $_ -match 'PostgreSQL_' } | 
    ForEach-Object { ($_ -replace '\s+\(Pending\)', '').Trim() }

# Mark PostgreSQL migrations as applied (don't actually run them)
if ($postgresqlMigrations.Count -gt 0) {
    Write-Host "`nMarking PostgreSQL migrations as applied (they are for Production only)..." -ForegroundColor Magenta
    
    $connectionString = "Server=LUNARIA\SQLEXPRESS;Database=EnterpriseGradeInventoryDB;Trusted_Connection=True;TrustServerCertificate=True;"
    
    foreach ($migration in $postgresqlMigrations) {
        Write-Host "  - $migration" -ForegroundColor Gray
        
        $sql = "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('$migration', '9.0.0')"
        
        try {
            sqlcmd -S "LUNARIA\SQLEXPRESS" -d "EnterpriseGradeInventoryDB" -E -Q $sql -h -1 2>&1 | Out-Null
        }
        catch {
            Write-Host "    Warning: Could not mark migration as applied" -ForegroundColor Yellow
        }
    }
}

# Now apply SQL Server migrations
Write-Host "`nApplying SQL Server migrations..." -ForegroundColor Cyan
dotnet ef database update

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Database updated successfully!" -ForegroundColor Green
} else {
    Write-Host "`n❌ Migration failed. Check errors above." -ForegroundColor Red
    exit 1
}
